import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { api } from '../api/dashboard.js'
import { RunSelect } from '../components/RunSelect.jsx'
import { ClusterSelect } from '../components/ClusterSelect.jsx'
import { ClusterInfoPanel } from '../features/validation/ClusterInfoPanel.jsx'
import { ValidationForm } from '../features/validation/ValidationForm.jsx'
import { ValidationSuccess } from '../features/validation/ValidationSuccess.jsx'

function defaultForm() {
    return {
        coherenceRating: 5,
        noveltyRating: 5,
        productPotentialRating: 5,
        action: 'Keep',
        notes: '',
        mergeTargetClusterId: null,
    }
}

export default function Validation() {
    const [searchParams] = useSearchParams()
    const queryClient = useQueryClient()

    const initialClusterId = searchParams.get('clusterId') ? Number(searchParams.get('clusterId')) : null
    const initialRunId = searchParams.get('runId') || null

    const [selectedRunId, setSelectedRunId] = useState(initialRunId)
    const [selectedClusterId, setSelectedClusterId] = useState(initialClusterId)
    const [form, setForm] = useState(defaultForm())
    const [submitted, setSubmitted] = useState(false)

    const { data: runs } = useQuery({ queryKey: ['runs'], queryFn: api.getRuns })
    const { data: opportunities } = useQuery({
        queryKey: ['opportunities', selectedRunId],
        queryFn: () => api.getOpportunities(selectedRunId),
    })

    const selectedCluster = opportunities?.clusters?.find(c => c.clusterId === selectedClusterId)
    const mergeTargetClusters = opportunities?.clusters?.filter(c => c.clusterId !== selectedClusterId) ?? []

    const mutation = useMutation({
        mutationFn: () => api.submitValidation(selectedClusterId, selectedRunId ?? opportunities?.clusterRunId, form),
        onSuccess: () => {
            setSubmitted(true)
            queryClient.invalidateQueries({ queryKey: ['opportunities'] })
        }
    })

    function handleRunChange(runId) {
        setSelectedRunId(runId)
        setSelectedClusterId(null)
        setForm(defaultForm())
        setSubmitted(false)
    }

    function handleClusterChange(value) {
        setSelectedClusterId(value ? Number(value) : null)
        setForm(defaultForm())
        setSubmitted(false)
    }

    function handleReset() {
        setSubmitted(false)
        setForm(defaultForm())
        setSelectedClusterId(null)
    }

    return (
        <div className="page">
            <div className="page-header">
                <h1 className="page-title">Validation Console</h1>
                <RunSelect selectedRunId={selectedRunId} onChange={handleRunChange} runs={runs} />
                <ClusterSelect
                    selectedClusterId={selectedClusterId}
                    onChange={handleClusterChange}
                    clusters={opportunities?.clusters}
                />
            </div>

            {!selectedClusterId && <div className="state">Select a cluster to validate.</div>}

            {selectedClusterId && selectedCluster && (
                <div className="validation-layout">
                    <ClusterInfoPanel cluster={selectedCluster} />
                    {submitted
                        ? <ValidationSuccess onReset={handleReset} />
                        : <ValidationForm
                            form={form}
                            setForm={setForm}
                            mergeTargetClusters={mergeTargetClusters}
                            mutation={mutation}
                            onSubmit={() => selectedClusterId && mutation.mutate()}
                          />
                    }
                </div>
            )}
        </div>
    )
}
