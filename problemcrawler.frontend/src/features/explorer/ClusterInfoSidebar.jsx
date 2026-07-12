export function ClusterInfoSidebar({ cluster }) {
    return (
        <div className="explorer-sidebar">
            <div className="sidebar-card">
                <div className="sidebar-label">Label</div>
                <div className="sidebar-value">{cluster.label}</div>
            </div>
            {cluster.description && (
                <div className="sidebar-card">
                    <div className="sidebar-label">Description</div>
                    <div className="sidebar-value small">{cluster.description}</div>
                </div>
            )}
            {cluster.opportunity && (
                <div className="sidebar-card">
                    <div className="sidebar-label">Opportunity</div>
                    {['true', 'false'].includes(cluster.opportunity.toLowerCase())
                        ? <div className="sidebar-value small">
                            {cluster.opportunity.toLowerCase() === 'true'
                                ? 'Identified as having a viable software solution angle.'
                                : 'No software opportunity identified.'}
                          </div>
                        : <div className="sidebar-value small">{cluster.opportunity}</div>
                    }
                </div>
            )}
            {cluster.mergedFromClusterIds?.length > 0 && (
                <div className="sidebar-card">
                    <div className="sidebar-label">Merged</div>
                    <div className="sidebar-value small">
                        Includes ideas from {cluster.mergedFromClusterIds.length} merged cluster{cluster.mergedFromClusterIds.length > 1 ? 's' : ''}.
                    </div>
                </div>
            )}
            <div className="sidebar-stats">
                <div className="stat">
                    <div className="stat-value">{cluster.size}</div>
                    <div className="stat-label">Ideas</div>
                </div>
                <div className="stat">
                    <div className="stat-value">{(cluster.avgConfidence * 100).toFixed(0)}%</div>
                    <div className="stat-label">Avg Confidence</div>
                </div>
            </div>
        </div>
    )
}
