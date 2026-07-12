import os
import sys
import uuid
import numpy as np
import umap
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
            "umap_projection": require_env("UMAP_PROJECTION_TABLE"),
        },
        "umap": {
            "n_components_cluster": int(optional_env("UMAP_N_COMPONENTS_CLUSTER", "10")),
            "n_neighbors":          int(optional_env("UMAP_N_NEIGHBORS", "30")),
            "min_dist_cluster":     float(optional_env("UMAP_MIN_DIST_CLUSTER", "0.02")),
            "min_dist_viz":         float(optional_env("UMAP_MIN_DIST_VIZ", "0.3")),
            "random_state":         int(optional_env("UMAP_RANDOM_STATE", "42")),
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
def run_umap_for_clustering(embeddings: np.ndarray, umap_config: dict) -> np.ndarray:
    print(f"[umap] Reducing {embeddings.shape[1]}D → {umap_config['n_components_cluster']}D for clustering...", flush=True)
    reducer = umap.UMAP(
        n_components=umap_config["n_components_cluster"],
        n_neighbors=umap_config["n_neighbors"],
        min_dist=umap_config["min_dist_cluster"],
        metric="cosine",
        random_state=umap_config["random_state"],
    )
    return reducer.fit_transform(embeddings).astype(np.float32)

def run_umap_for_visualization(embeddings: np.ndarray, umap_config: dict) -> np.ndarray:
    print(f"[umap] Reducing {embeddings.shape[1]}D → 2D for visualization...", flush=True)
    reducer = umap.UMAP(
        n_components=2,
        n_neighbors=umap_config["n_neighbors"],
        min_dist=umap_config["min_dist_viz"],
        metric="cosine",
        random_state=umap_config["random_state"],
    )
    return reducer.fit_transform(embeddings).astype(np.float32)

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
            i."RawJson" || jsonb_build_object(
                'supportingMentionCount',       i."SupportingMentionCount",
                'supportingDistinctAuthorCount', i."SupportingDistinctAuthorCount"
            ),
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
    """, (run_id, "hdbscan+umap", min_cluster_size, min_samples))

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
def save_umap_projections(cur, table: dict, run_id: str, ids: list, coords: np.ndarray) -> None:
    batch = [
        (str(uuid.uuid4()), run_id, str(ids[i]), float(coords[i, 0]), float(coords[i, 1]))
        for i in range(len(ids))
    ]
    cur.executemany(f"""
        INSERT INTO {quote_ident(table["umap_projection"])}
            ("Id", "ClusterRunId", "ThreadSynthesizedIdeaEmbeddingId", "X", "Y")
        VALUES (%s, %s, %s, %s, %s)
    """, batch)
    print(f"[umap] Saved {len(batch)} 2D projections", flush=True)
def find_centroid_representatives(ids: list, embeddings_reduced: np.ndarray, labels) -> dict:
    representatives = {}
    for label in set(labels):
        if label == -1:
            continue
        indices = np.where(labels == label)[0]
        cluster_embs = embeddings_reduced[indices]
        centroid = cluster_embs.mean(axis=0)
        closest = int(np.argmin(np.linalg.norm(cluster_embs - centroid, axis=1)))
        representatives[int(label)] = ids[indices[closest]]
    print(f"[cluster] Found centroid representatives for {len(representatives)} clusters", flush=True)
    return representatives

def save_cluster_labels(cur, table: dict, run_id: str, representatives: dict) -> None:
    if not representatives:
        print("[cluster] No centroid representatives to label", flush=True)
        return

    batch = [(str(emb_id), run_id, label) for label, emb_id in representatives.items()]
    cur.executemany(f"""
        UPDATE {quote_ident(table["problem_cluster"])} pc
        SET
            "Label"       = i."ProblemSummary",
            "Description" = i."ProblemDetails",
            "Opportunity" = i."ActionabilityRationale"
        FROM {quote_ident(table["source"])} e
        JOIN {quote_ident(table["idea"])} i
            ON i."Id" = e."ThreadSynthesizedIdeaId"
        WHERE e.{quote_ident(table["id"])} = %s::uuid
          AND pc."ClusterRunId" = %s
          AND pc."ClusterId"    = %s
    """, batch)
    print(f"[cluster] Labelled {len(batch)} clusters from centroid representatives", flush=True)
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
        cluster_selection_method="leaf"
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

            embeddings_reduced = run_umap_for_clustering(embeddings, config["umap"])
            labels, probabilities = run_clustering(embeddings_reduced, config["hdbscan"])

            has_clusters = any(label != -1 for label in labels)

            representatives = find_centroid_representatives(ids, embeddings_reduced, labels)

            if has_clusters:
                snapped = snapshot_state(cur, config["table"], run_id)
                print(f"[cluster] Snapshotted {snapped} embeddings", flush=True)
            else:
                print("[cluster] No clusters found, skipping embedding archive", flush=True)

            save_cluster_assignments(cur, config["table"], run_id, ids, labels, probabilities)
            if has_clusters:
                coords_2d = run_umap_for_visualization(embeddings_reduced, config["umap"])
                save_umap_projections(cur, config["table"], run_id, ids, coords_2d)

            n_clusters = save_problem_clusters(cur, config["table"], run_id, labels, probabilities)
            print(f"[cluster] Saved {n_clusters} clusters for run {run_id}", flush=True)
            save_cluster_labels(cur, config["table"], run_id, representatives)
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