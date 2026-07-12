function CheckLabel({ checked, onChange, children }) {
    return (
        <label className="atlas-check-label">
            <input type="checkbox" checked={checked} onChange={onChange} />
            {children}
        </label>
    )
}

function ModeButton({ active, onClick, children }) {
    return (
        <button className={`atlas-mode-btn${active ? ' atlas-mode-btn--active' : ''}`} onClick={onClick}>
            {children}
        </button>
    )
}

export function AtlasToolbar({
    viewMode, setViewMode,
    minConfidence, setMinConfidence,
    hasPins,
    highlightOnly, setHighlightOnly,
    hideUnpinned, setHideUnpinned,
    pinnedClusterIds, clearPin,
    showNoise, setShowNoise,
    selectedRunId, setSelectedRunId,
    runs,
}) {
    return (
        <div className="atlas-toolbar">
            <div className="atlas-toolbar-left">
                <h1 className="atlas-title">Cluster Atlas</h1>

                <div className="atlas-mode-switcher">
                    <ModeButton active={viewMode === 'scatter'} onClick={() => setViewMode('scatter')}>Scatter View</ModeButton>
                    <ModeButton active={viewMode === 'bubble'} onClick={() => setViewMode('bubble')}>Bubble View</ModeButton>
                </div>

                <div className="atlas-confidence">
                    <span className="atlas-confidence-label">Min Confidence:</span>
                    <input
                        type="range" min="0.0" max="0.95" step="0.05"
                        value={minConfidence}
                        onChange={e => setMinConfidence(parseFloat(e.target.value))}
                        className="atlas-confidence-slider"
                    />
                    <span className="atlas-confidence-value">
                        {(minConfidence * 100).toFixed(0)}%
                    </span>
                </div>
            </div>

            <div className="atlas-toolbar-right">
                {hasPins && (
                    <>
                        <CheckLabel checked={highlightOnly} onChange={e => setHighlightOnly(e.target.checked)}>Highlight only</CheckLabel>
                        <CheckLabel checked={hideUnpinned} onChange={e => setHideUnpinned(e.target.checked)}>Hide unpinned</CheckLabel>
                        <button className="atlas-clear-btn" onClick={clearPin}>
                            Clear {pinnedClusterIds.size} pinned
                        </button>
                    </>
                )}
                <CheckLabel checked={showNoise} onChange={e => setShowNoise(e.target.checked)}>Show Noise Points</CheckLabel>
                <select
                    className="atlas-run-select"
                    value={selectedRunId ?? ''}
                    onChange={e => setSelectedRunId(e.target.value || null)}
                >
                    <option value="">Latest run</option>
                    {runs?.map(r => (
                        <option key={r.id} value={r.id}>
                            {new Date(r.createdAtUtc).toLocaleString()} — {r.totalClusters} clusters
                        </option>
                    ))}
                </select>
            </div>
        </div>
    )
}
