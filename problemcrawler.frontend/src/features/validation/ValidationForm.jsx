import { ACTIONS } from '../../constants/validation.js'
import { RatingField } from './RatingField.jsx'

export function ValidationForm({ form, setForm, mergeTargetClusters, mutation, onSubmit }) {
    return (
        <div className="validation-form">
            <RatingField
                label="Coherence"
                description="Do these ideas belong together?"
                value={form.coherenceRating}
                onChange={v => setForm(f => ({ ...f, coherenceRating: v }))}
            />
            <RatingField
                label="Novelty"
                description="Is this problem space surprising or obvious?"
                value={form.noveltyRating}
                onChange={v => setForm(f => ({ ...f, noveltyRating: v }))}
            />
            <RatingField
                label="Product Potential"
                description="Would we build for this?"
                value={form.productPotentialRating}
                onChange={v => setForm(f => ({ ...f, productPotentialRating: v }))}
            />

            <div className="form-field">
                <div className="form-label">Action</div>
                <div className="action-grid">
                    {ACTIONS.map(a => (
                        <button
                            key={a.value}
                            className={`action-btn ${form.action === a.value ? 'active' : ''}`}
                            onClick={() => setForm(f => ({ ...f, action: a.value, mergeTargetClusterId: null }))}
                        >
                            <div className="action-label">{a.label}</div>
                            <div className="action-desc">{a.description}</div>
                        </button>
                    ))}
                </div>
            </div>

            {form.action === 'Merge' && (
                <div className="form-field">
                    <div className="form-label">Merge into <span className="form-optional">(required)</span></div>
                    <select
                        className="run-select"
                        value={form.mergeTargetClusterId ?? ''}
                        onChange={e => setForm(f => ({ ...f, mergeTargetClusterId: e.target.value ? Number(e.target.value) : null }))}
                    >
                        <option value="">Select target cluster</option>
                        {mergeTargetClusters.map(c => (
                            <option key={c.clusterId} value={c.clusterId}>
                                {c.label} ({c.size} ideas)
                            </option>
                        ))}
                    </select>
                </div>
            )}

            <div className="form-field">
                <div className="form-label">Notes <span className="form-optional">(optional)</span></div>
                <textarea
                    className="val-textarea"
                    value={form.notes}
                    onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                    placeholder="Any additional observations..."
                    rows={3}
                />
            </div>

            <div className="form-actions">
                <button
                    className="btn btn-primary"
                    onClick={onSubmit}
                    disabled={mutation.isPending || (form.action === 'Merge' && !form.mergeTargetClusterId)}
                >
                    {mutation.isPending ? 'Saving...' : 'Save validation'}
                </button>
                {mutation.isError && (
                    <span className="form-error">Failed to save. Try again.</span>
                )}
            </div>
        </div>
    )
}
