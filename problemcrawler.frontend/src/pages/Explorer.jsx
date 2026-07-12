import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { api } from '../api/dashboard.js'
import { RunSelect } from '../components/RunSelect.jsx'
import { ClusterSelect } from '../components/ClusterSelect.jsx'
import { ClusterInfoSidebar } from '../features/explorer/ClusterInfoSidebar.jsx'
import { IdeasPanel } from '../features/explorer/IdeasPanel.jsx'

export default function Explorer() {
    const [searchParams] = useSearchParams()
    const navigate = useNavigate()

    const initialClusterId = searchParams.get('clusterId') ? Number(searchParams.get('clusterId')) : null
    const initialRunId = searchParams.get('runId') || null

    const [selectedRunId, setSelectedRunId] = useState(initialRunId)
    const [selectedClusterId, setSelectedClusterId] = useState(initialClusterId)

    const { data: runs } = useQuery({ queryKey: ['runs'], queryFn: api.getRuns })
    const { data: opportunities } = useQuery({
        queryKey: ['opportunities', selectedRunId],
        queryFn: () => api.getOpportunities(selectedRunId),
    })
    const { data: detail, isLoading, isError } = useQuery({
        queryKey: ['cluster', selectedClusterId, selectedRunId],
        queryFn: () => api.getCluster(selectedClusterId, selectedRunId),
        enabled: selectedClusterId !== null,
    })

    function handleRunChange(runId) {
        setSelectedRunId(runId)
        setSelectedClusterId(null)
    }

    function handleClusterChange(value) {
        setSelectedClusterId(value ? Number(value) : null)
    }

    return (
        <div className="page">
            <div className="page-header">
                <h1 className="page-title">Cluster Explorer</h1>
                <RunSelect selectedRunId={selectedRunId} onChange={handleRunChange} runs={runs} />
                <ClusterSelect
                    selectedClusterId={selectedClusterId}
                    onChange={handleClusterChange}
                    clusters={opportunities?.clusters}
                />
                {selectedClusterId !== null && (
                    <button
                        className="btn"
                        onClick={() => navigate(`/validation?clusterId=${selectedClusterId}&runId=${selectedRunId ?? ''}`)}
                    >
                        Validate this cluster
                    </button>
                )}
            </div>

            {!selectedClusterId && <div className="state">Select a cluster to inspect it.</div>}
            {selectedClusterId && isLoading && <div className="state">Loading...</div>}
            {selectedClusterId && isError && <div className="state error">Failed to load cluster.</div>}

            {detail && (
                <div className="explorer-layout">
                    <ClusterInfoSidebar cluster={detail.cluster} />
                    <IdeasPanel key={selectedClusterId} ideas={detail.cluster.ideas} />
                </div>
            )}
        </div>
    )
}
