export function RunSelect({ selectedRunId, onChange, runs }) {
    return (
        <select
            className="run-select"
            value={selectedRunId ?? ''}
            onChange={e => onChange(e.target.value || null)}
        >
            <option value="">Latest run</option>
            {runs?.map(r => (
                <option key={r.id} value={r.id}>
                    {new Date(r.createdAtUtc).toLocaleString()} — {r.totalClusters} clusters
                </option>
            ))}
        </select>
    )
}
