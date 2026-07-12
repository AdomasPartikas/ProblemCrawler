export function IdeaCard({ idea, isExpanded, onToggle }) {
    const { snap } = idea
    const confPct = idea.clusterConfidence != null ? (idea.clusterConfidence * 100).toFixed(0) : null

    return (
        <div className={`idea-card${snap?.urgencySignal ? ` urgency-${snap.urgencySignal}` : ''}`}>
            <div className="idea-card-header" onClick={onToggle}>
                <div className={`idea-expand-icon${isExpanded ? ' open' : ''}`}>▼</div>
                <div className="idea-summary">{snap?.problemSummary ?? 'No summary'}</div>
                <div className="idea-meta">
                    {snap?.urgencySignal && (
                        <span className={`badge urgency-${snap.urgencySignal}`}>{snap.urgencySignal}</span>
                    )}
                    {snap?.softwareOpportunity && (
                        <span className="badge badge-software">software</span>
                    )}
                    <div className="idea-conf-track">
                        <div className="idea-conf-fill" style={{ width: `${confPct ?? 0}%` }} />
                    </div>
                    <span className="idea-conf">{confPct != null ? confPct + '%' : '—'}</span>
                </div>
            </div>

            {isExpanded && snap && (
                <div className="idea-expanded">
                    {snap.problemDetails && (
                        <div className="idea-field">
                            <div className="idea-field-label">Details</div>
                            <div className="idea-field-value">{snap.problemDetails}</div>
                        </div>
                    )}
                    {snap.actor && (
                        <div className="idea-field">
                            <div className="idea-field-label">Actor</div>
                            <div className="idea-field-value">{snap.actor}</div>
                        </div>
                    )}
                    {snap.industry && (
                        <div className="idea-field">
                            <div className="idea-field-label">Industry</div>
                            <div className="idea-field-value">{snap.industry}</div>
                        </div>
                    )}
                    {snap.currentWorkaround && (
                        <div className="idea-field">
                            <div className="idea-field-label">Current Workaround</div>
                            <div className="idea-field-value">{snap.currentWorkaround}</div>
                        </div>
                    )}
                    {snap.desiredOutcome && (
                        <div className="idea-field">
                            <div className="idea-field-label">Desired Outcome</div>
                            <div className="idea-field-value">{snap.desiredOutcome}</div>
                        </div>
                    )}
                    {snap.actionabilityRationale && (
                        <div className="idea-field">
                            <div className="idea-field-label">Actionability</div>
                            <div className="idea-field-value">{snap.actionabilityRationale}</div>
                        </div>
                    )}
                    <div className="idea-counts">
                        {snap.supportingMentionCount > 0 && <span>{snap.supportingMentionCount} mentions</span>}
                        {snap.supportingDistinctAuthorCount > 0 && <span>{snap.supportingDistinctAuthorCount} authors</span>}
                    </div>
                </div>
            )}
        </div>
    )
}
