export function ClusterSidebar({ clusterIds, pinnedClusterIds, hoveredClusterId, colorMap, hasPins, togglePin, setHoveredClusterId }) {
    return (
        <div className="atlas-sidebar">
            <div className="atlas-sidebar-header">
                CLICK TO PIN ({clusterIds.length})
            </div>
            <div className="atlas-sidebar-list">
                {clusterIds.map(id => {
                    const isPinned = pinnedClusterIds.has(id)
                    const isHovered = hoveredClusterId === id
                    return (
                        <div
                            key={id}
                            className={`atlas-cluster-item${isPinned ? ' atlas-cluster-item--pinned' : ''}${isHovered ? ' atlas-cluster-item--hovered' : ''}`}
                            onMouseEnter={() => !hasPins && setHoveredClusterId(id)}
                            onMouseLeave={() => !hasPins && setHoveredClusterId(null)}
                            onClick={() => togglePin(id)}
                        >
                            <div className="atlas-cluster-dot" style={{ background: colorMap.get(id) }} />
                            <span className={`atlas-cluster-name${isPinned || isHovered ? ' atlas-cluster-name--active' : ''}`}>
                                Cluster {id} {isPinned && '📌'}
                            </span>
                        </div>
                    )
                })}
            </div>
        </div>
    )
}
