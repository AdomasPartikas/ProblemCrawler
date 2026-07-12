import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell } from 'recharts'
import { SCORE_FIELDS } from '../../constants/opportunities.js'

export function OpportunityCard({ cluster, idx, sortBy, isExpanded, onToggle, onExplore, onValidate }) {
    return (
        <div className="opp-card">
            <div className="opp-card-header" onClick={onToggle}>
                <div className="opp-rank">#{idx + 1}</div>
                <div className="opp-meta">
                    <div className="opp-label">{cluster.label}</div>
                    {cluster.description && (
                        <div className="opp-description">{cluster.description}</div>
                    )}
                </div>
                <div className="opp-score-preview">
                    {SCORE_FIELDS.map(f => (
                        <div
                            key={f.key}
                            className={`opp-mini-bar${sortBy === f.key ? ' sorted' : ''}`}
                            title={`${f.label}: ${(cluster[f.key] * 100).toFixed(0)}%`}
                        >
                            <div
                                className="opp-mini-bar-fill"
                                style={{ height: `${(cluster[f.key] * 100).toFixed(0)}%`, background: f.color }}
                            />
                        </div>
                    ))}
                </div>
                <div className="opp-stats">
                    <span className="opp-size">{cluster.size} ideas</span>
                    <span className="opp-score">{(cluster.opportunityScore * 100).toFixed(1)}</span>
                </div>
                <div className={`opp-expand-icon${isExpanded ? ' open' : ''}`}>▼</div>
            </div>

            {isExpanded && (
                <div className="opp-expanded">
                    {cluster.opportunity && (
                        ['true', 'false'].includes(cluster.opportunity.toLowerCase())
                            ? cluster.opportunity.toLowerCase() === 'true' && (
                                <div className="opp-opportunity opp-opportunity--flag">
                                    <span className="badge badge-software">software opportunity</span>
                                    <span>This cluster has been identified as having a viable software solution angle.</span>
                                </div>
                            )
                            : <div className="opp-opportunity">{cluster.opportunity}</div>
                    )}
                    <div className="opp-chart">
                        <ResponsiveContainer width="100%" height={160}>
                            <BarChart
                                data={SCORE_FIELDS.map(f => ({ name: f.label, value: cluster[f.key], color: f.color }))}
                                layout="vertical"
                                margin={{ top: 4, right: 12, bottom: 4, left: 0 }}
                            >
                                <XAxis type="number" domain={[0, 1]} tick={{ fontSize: 11, fill: '#888' }} tickFormatter={v => `${(v * 100).toFixed(0)}%`} />
                                <YAxis type="category" dataKey="name" tick={{ fontSize: 12, fill: '#ccc' }} width={80} />
                                <Tooltip
                                    formatter={v => `${(v * 100).toFixed(1)}%`}
                                    contentStyle={{ background: '#1e1e1e', border: '1px solid #333', borderRadius: 6 }}
                                    labelStyle={{ color: '#ccc', fontWeight: 600, fontSize: 12 }}
                                    itemStyle={{ color: '#e0e0e0', fontSize: 12 }}
                                    cursor={{ fill: 'rgba(255,255,255,0.04)' }}
                                />
                                <Bar dataKey="value" radius={[0, 4, 4, 0]}>
                                    {SCORE_FIELDS.map(f => <Cell key={f.key} fill={f.color} />)}
                                </Bar>
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                    <div className="opp-actions">
                        <button className="btn" onClick={onExplore}>Explore</button>
                        <button className="btn" onClick={onValidate}>Validate</button>
                    </div>
                </div>
            )}
        </div>
    )
}
