export function RatingField({ label, description, value, onChange }) {
    return (
        <div className="form-field">
            <div className="form-label">
                {label}
                <span className="form-desc">{description}</span>
            </div>
            <div className="rating-row">
                {Array.from({ length: 10 }, (_, i) => i + 1).map(n => (
                    <button
                        key={n}
                        className={`rating-btn ${value === n ? 'active' : ''}`}
                        onClick={() => onChange(n)}
                    >
                        {n}
                    </button>
                ))}
                <span className="rating-value">{value}/10</span>
            </div>
        </div>
    )
}
