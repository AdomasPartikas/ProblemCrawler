import { SCORE_FIELDS } from '../../constants/opportunities.js'

export function ScoreLegend() {
    return (
        <div className="score-legend">
            {SCORE_FIELDS.map(f => (
                <div key={f.key} className="score-legend-item">
                    <div className="score-legend-dot" style={{ background: f.color }} />
                    <div>
                        <span className="score-legend-label">{f.label}</span>
                        <span className="score-legend-desc">{f.description}</span>
                    </div>
                </div>
            ))}
        </div>
    )
}
