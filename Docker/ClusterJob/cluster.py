import os
import sys
import uuid
import numpy as np
import psycopg2
from pgvector.psycopg2 import register_vector
import hdbscan

sys.stdout.reconfigure(line_buffering=True)
sys.stderr.reconfigure(line_buffering=True)

def require_env(name: str) -> str:
    value = os.getenv(name)
    if not value:
        raise RuntimeError(f"Missing required env var: {name}")
    return value

def optional_env(name: str, default: str = None) -> str:
    return os.getenv(name, default)

def optional_int(name: str) -> int | None:
    value = os.getenv(name)
    return int(value) if value else None

def quote_ident(name: str) -> str:
    return '"' + name.replace('"', '""') + '"'

def load_config() -> dict:
    return {
        "db": {
            "host":     require_env("DB_HOST"),
            "port":     optional_env("DB_PORT", "5432"),
            "dbname":   require_env("DB_NAME"),
            "user":     require_env("DB_USER"),
            "password": require_env("DB_PASSWORD"),
            "sslmode":  require_env("DB_SSLMODE"),
        },
        "schema": require_env("DB_SCHEMA"),
        "table": {
            "source":                   require_env("SOURCE_TABLE"),
            "id":                       require_env("ID_COLUMN"),
            "embedding":                require_env("EMBEDDING_COLUMN"),
            "cluster_id":               require_env("CLUSTER_ID_COLUMN"),
            "cluster_conf":             require_env("CLUSTER_CONFIDENCE_COLUMN"),
            "cluster_run_id":           require_env("CLUSTER_RUN_ID_COLUMN"),
            "cluster_run":              require_env("CLUSTER_RUN_TABLE"),
            "problem_cluster":          require_env("PROBLEM_CLUSTER_TABLE"),
            "idea":                     require_env("IDEA_TABLE"),
            "embedding_archive":        require_env("EMBEDDING_ARCHIVE_TABLE"),
            "problem_cluster_archive":  require_env("PROBLEM_CLUSTER_ARCHIVE_TABLE"),
        },
        "hdbscan": {
            "min_cluster_size": int(require_env("HDBSCAN_MIN_CLUSTER_SIZE")),
            "min_samples":      optional_int("HDBSCAN_MIN_SAMPLES"),
        },
        "max_cluster_runs": int(optional_env("MAX_CLUSTER_RUNS", "10")),
    }

def connect(db_config: dict, schema: str) -> psycopg2.extensions.connection:
    conn = psycopg2.connect(**db_config)
    register_vector(conn)
    with conn.cursor() as cur:
        cur.execute(f'SET search_path TO "{schema}"')
    return conn

def qualify(schema: str, name: str) -> str:
    return f'{quote_ident(schema)}.{quote_ident(name)}'

def fetch_embeddings(cur, schema: str, table: dict) -> tuple[list, np.ndarray]:
    cur.execute(f"""
        SELECT {quote_ident(table["id"])},
               {quote_ident(table["embedding"])}
        FROM   {qualify(schema, table["source"])}
        WHERE  {quote_ident(table["embedding"])} IS NOT NULL
    """)
    rows = cur.fetchall()
    if not rows:
        return [], np.empty((0,))

    ids        = [r[0] for r in rows]
    embeddings = np.array([r[1] for r in rows], dtype=np.float32)
    return ids, embeddings

def snapshot_state(cur, table: dict, run_id: str) -> int:

    cur.execute(f"""
        INSERT INTO {quote_ident(table["embedding_archive"])}
            ("Id", "ClusterRunId", "ThreadSynthesizedIdeaId",
             "Model", "Embedding", "ClusterId", "ClusterConfidence",
             "IdeaSnapshot", "CreatedAtUtc")
        SELECT
            gen_random_uuid(),
            %s,
            e."ThreadSynthesizedIdeaId",
            e."Model",
            e."Embedding",
            e."ClusterId",
            e."ClusterConfidence",
            i."RawJson",
            NOW()
        FROM {quote_ident(table["source"])} e
        JOIN {quote_ident(table["idea"])} i
            ON i."Id" = e."ThreadSynthesizedIdeaId"
        WHERE e."Embedding" IS NOT NULL
    """, (run_id,))
    return cur.rowcount

def insert_cluster_run(cur, table: dict, run_id: str, min_cluster_size: int, min_samples: int | None) -> None:
    cur.execute(f"""
        INSERT INTO {quote_ident(table["cluster_run"])}
            ("Id", "Algorithm", "MinClusterSize", "MinSamples", "IsPinned", "CreatedAtUtc")
        VALUES (%s, %s, %s, %s, FALSE, NOW())
    """, (run_id, "hdbscan", min_cluster_size, min_samples))

def save_cluster_assignments(cur, table: dict, run_id: str, ids: list, labels, probabilities) -> None:
    batch = [
        (int(labels[i]), float(probabilities[i]), run_id, ids[i])
        for i in range(len(ids))
    ]
    cur.executemany(f"""
        UPDATE {quote_ident(table["source"])}
        SET    {quote_ident(table["cluster_id"])}     = %s,
               {quote_ident(table["cluster_conf"])}   = %s,
               {quote_ident(table["cluster_run_id"])} = %s
        WHERE  {quote_ident(table["id"])}             = %s
    """, batch)

def save_problem_clusters(cur, table: dict, run_id: str, labels, probabilities) -> int:
    unique_labels = [l for l in set(labels) if l != -1]
    if not unique_labels:
        return 0

    batch = []
    for label in unique_labels:
        mask     = labels == label
        size     = int(mask.sum())
        avg_conf = float(probabilities[mask].mean())
        batch.append((str(uuid.uuid4()), run_id, int(label), size, avg_conf))

    cur.executemany(f"""
        INSERT INTO {quote_ident(table["problem_cluster"])}
            ("Id", "ClusterRunId", "ClusterId", "Size", "AvgConfidence", "CreatedAtUtc")
        VALUES (%s, %s, %s, %s, %s, NOW())
    """, batch)

    return len(batch)

def save_problem_cluster_archive(cur, table: dict, run_id: str, labels, probabilities) -> None:
    unique_labels = [l for l in set(labels) if l != -1]
    if not unique_labels:
        return

    batch = []
    for label in unique_labels:
        mask     = labels == label
        size     = int(mask.sum())
        avg_conf = float(probabilities[mask].mean())
        batch.append((str(uuid.uuid4()), run_id, int(label), size, avg_conf))

    cur.executemany(f"""
        INSERT INTO {quote_ident(table["problem_cluster_archive"])}
            ("Id", "ClusterRunId", "ClusterId", "Size", "AvgConfidence", "CreatedAtUtc")
        VALUES (%s, %s, %s, %s, %s, NOW())
    """, batch)

def prune_old_runs(cur, table: dict, keep: int) -> int:
    cur.execute(f"""
        WITH ranked AS (
            SELECT "Id",
                   ROW_NUMBER() OVER (ORDER BY "CreatedAtUtc" DESC) AS rn
            FROM   {quote_ident(table["cluster_run"])}
            WHERE  "IsPinned" = FALSE
        ),
        to_delete AS (
            SELECT "Id" FROM ranked WHERE rn > %s
        )
        DELETE FROM {quote_ident(table["cluster_run"])}
        WHERE "Id" IN (SELECT "Id" FROM to_delete)
    """, (keep,))
    return cur.rowcount

def run_clustering(embeddings: np.ndarray, hdbscan_config: dict):
    norms = np.linalg.norm(embeddings, axis=1, keepdims=True)
    norms = np.where(norms == 0, 1, norms)
    embeddings = embeddings / norms

    clusterer = hdbscan.HDBSCAN(
        min_cluster_size=hdbscan_config["min_cluster_size"],
        min_samples=hdbscan_config["min_samples"],
        metric="euclidean",
    )
    labels = clusterer.fit_predict(embeddings)
    probabilities = clusterer.probabilities_

    n_clusters = len(set(labels)) - (1 if -1 in labels else 0)
    n_noise = int((labels == -1).sum())
    print(f"[cluster] Found {n_clusters} clusters, {n_noise} noise points", flush=True)

    return labels, probabilities

def main() -> int:
    config = load_config()
    run_id = str(uuid.uuid4())

    with connect(config["db"], config["schema"]) as conn:
        with conn.cursor() as cur:
           
            ids, embeddings = fetch_embeddings(cur, config["schema"], config["table"])

            if len(ids) == 0:
                print("[cluster] No embeddings found, exiting", flush=True)
                return 0

            print(f"[cluster] Loaded {len(ids)} embeddings", flush=True)

            insert_cluster_run(cur, config["table"], run_id,
                   config["hdbscan"]["min_cluster_size"],
                   config["hdbscan"]["min_samples"])
            conn.commit()

            labels, probabilities = run_clustering(embeddings, config["hdbscan"])

            has_clusters = any(label != -1 for label in labels)

            if has_clusters:
                snapped = snapshot_state(cur, config["table"], run_id)
                print(f"[cluster] Snapshotted {snapped} embeddings", flush=True)
            else:
                print("[cluster] No clusters found, skipping embedding archive", flush=True)

            save_cluster_assignments(cur, config["table"], run_id, ids, labels, probabilities)

            n_clusters = save_problem_clusters(cur, config["table"], run_id, labels, probabilities)
            print(f"[cluster] Saved {n_clusters} clusters for run {run_id}", flush=True)

            save_problem_cluster_archive(cur, config["table"], run_id, labels, probabilities)
            print(f"[cluster] Archived {n_clusters} clusters", flush=True)

            pruned = prune_old_runs(cur, config["table"], config["max_cluster_runs"])
            print(f"[cluster] Pruned {pruned} old cluster runs", flush=True)

        conn.commit()
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as ex:
        import traceback
        traceback.print_exc()
        raise