export function ScorePill({ label, value, highlight }) {
    return (
        <div className={`score-pill ${highlight ? 'highlight' : ''}`}>
            <div className="score-pill-label">{label}</div>
            <div className="score-pill-value">{(value * 100).toFixed(0)}</div>
        </div>
    )
}
