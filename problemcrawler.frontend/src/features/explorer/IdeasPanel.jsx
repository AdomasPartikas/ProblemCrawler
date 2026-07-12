import { useState } from 'react'
import { IdeaCard } from './IdeaCard.jsx'
import { SORT_OPTIONS, URGENCY_RANK } from '../../constants/explorer.js'

function parseSnapshot(raw) {
    try { return JSON.parse(raw) } catch { return null }
}

export function IdeasPanel({ ideas }) {
    const [sortBy, setSortBy] = useState('confidence')
    const [expandedIdx, setExpandedIdx] = useState(null)

    const sorted = [...ideas]
        .map(idea => ({ ...idea, snap: parseSnapshot(idea.ideaSnapshot) }))
        .sort((a, b) => {
            if (sortBy === 'urgency')
                return (URGENCY_RANK[b.snap?.urgencySignal] ?? 0) - (URGENCY_RANK[a.snap?.urgencySignal] ?? 0)
            if (sortBy === 'mentions')
                return (b.snap?.supportingMentionCount ?? 0) - (a.snap?.supportingMentionCount ?? 0)
            if (sortBy === 'authors')
                return (b.snap?.supportingDistinctAuthorCount ?? 0) - (a.snap?.supportingDistinctAuthorCount ?? 0)
            return (b.clusterConfidence ?? 0) - (a.clusterConfidence ?? 0)
        })

    return (
        <div className="explorer-ideas">
            <div className="ideas-header">
                <span>{sorted.length} ideas</span>
                <div className="ideas-sort">
                    <span className="ideas-sort-label">Sort by</span>
                    <select
                        className="ideas-sort-select"
                        value={sortBy}
                        onChange={e => setSortBy(e.target.value)}
                    >
                        {SORT_OPTIONS.map(o => (
                            <option key={o.value} value={o.value}>{o.label}</option>
                        ))}
                    </select>
                </div>
            </div>
            <div className="ideas-legend">
                <div className="legend-item">
                    <div className="legend-dot urgency-high" />
                    <div className="legend-dot urgency-medium" />
                    <div className="legend-dot urgency-low" />
                    <span>Urgency</span>
                </div>
                <div className="legend-sep" />
                <div className="legend-item">
                    <span className="badge badge-software">software</span>
                    <span>Software opportunity</span>
                </div>
                <div className="legend-sep" />
                <div className="legend-item">
                    <div className="legend-conf-track"><div className="legend-conf-fill" /></div>
                    <span>Confidence</span>
                </div>
            </div>
            <div className="ideas-scroll">
                {sorted.map((idea, idx) => (
                    <IdeaCard
                        key={idea.threadSynthesizedIdeaId}
                        idea={idea}
                        isExpanded={expandedIdx === idx}
                        onToggle={() => setExpandedIdx(expandedIdx === idx ? null : idx)}
                    />
                ))}
            </div>
        </div>
    )
}
