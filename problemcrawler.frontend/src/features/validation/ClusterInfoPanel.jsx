import { ScorePill } from './ScorePill.jsx'

export function ClusterInfoPanel({ cluster }) {
    return (
        <div className="validation-cluster-info">
            <div className="val-label">{cluster.label}</div>
            {cluster.description && (
                <div className="val-description">{cluster.description}</div>
            )}
            <div className="val-scores">
                <ScorePill label="Pain"      value={cluster.painIntensityScore} />
                <ScorePill label="Frequency" value={cluster.mentionFrequencyScore} />
                <ScorePill label="Vacuum"    value={cluster.solutionVacuumScore} />
                <ScorePill label="Market"    value={cluster.softwareMarketScore} />
                <ScorePill label="Authors"   value={cluster.authorBreadthScore} />
                <ScorePill label="Overall"   value={cluster.opportunityScore} highlight />
            </div>
        </div>
    )
}
