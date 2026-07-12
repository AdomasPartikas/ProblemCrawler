export function ClusterSelect({ selectedClusterId, onChange, clusters }) {
    return (
        <select
            className="run-select"
            value={selectedClusterId ?? ''}
            onChange={e => onChange(e.target.value)}
        >
            <option value="">Select cluster</option>
            {clusters?.map(c => (
                <option key={c.clusterId} value={c.clusterId}>
                    {c.label} ({c.size} ideas)
                </option>
            ))}
        </select>
    )
}
