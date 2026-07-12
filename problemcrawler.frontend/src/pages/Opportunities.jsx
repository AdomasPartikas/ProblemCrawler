import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/dashboard.js'
import { RunSelect } from '../components/RunSelect.jsx'
import { SORT_OPTIONS } from '../constants/opportunities.js'
import { ScoreLegend } from '../features/opportunities/ScoreLegend.jsx'
import { OpportunityCard } from '../features/opportunities/OpportunityCard.jsx'

export default function Opportunities() {
    const navigate = useNavigate()
    const [selectedRunId, setSelectedRunId] = useState(null)
    const [sortBy, setSortBy] = useState('opportunityScore')
    const [expandedId, setExpandedId] = useState(null)

    const { data: runs } = useQuery({ queryKey: ['runs'], queryFn: api.getRuns })
    const { data, isLoading, isError } = useQuery({
        queryKey: ['opportunities', selectedRunId],
        queryFn: () => api.getOpportunities(selectedRunId),
    })

    const sorted = [...(data?.clusters ?? [])].sort((a, b) => b[sortBy] - a[sortBy])

    return (
        <div className="page">
            <div className="page-header">
                <h1 className="page-title">Opportunity Ranking</h1>
                <RunSelect selectedRunId={selectedRunId} onChange={setSelectedRunId} runs={runs} />
                <div className="sort-row">
                    <span className="sort-label">Sort by</span>
                    <select className="run-select" value={sortBy} onChange={e => setSortBy(e.target.value)}>
                        {SORT_OPTIONS.map(o => (
                            <option key={o.value} value={o.value}>{o.label}</option>
                        ))}
                    </select>
                </div>
            </div>

            <ScoreLegend />

            {isLoading && <div className="state">Loading...</div>}
            {isError && <div className="state error">Failed to load opportunities.</div>}

            {!isLoading && !isError && (
                <div className="opp-list">
                    {sorted.map((cluster, idx) => (
                        <OpportunityCard
                            key={cluster.clusterId}
                            cluster={cluster}
                            idx={idx}
                            sortBy={sortBy}
                            isExpanded={expandedId === cluster.clusterId}
                            onToggle={() => setExpandedId(expandedId === cluster.clusterId ? null : cluster.clusterId)}
                            onExplore={() => navigate(`/explorer?clusterId=${cluster.clusterId}&runId=${data.clusterRunId}`)}
                            onValidate={() => navigate(`/validation?clusterId=${cluster.clusterId}&runId=${data.clusterRunId}`)}
                        />
                    ))}
                </div>
            )}
        </div>
    )
}
