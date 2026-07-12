export function ValidationSuccess({ onReset }) {
    return (
        <div className="validation-success">
            <div className="success-icon">✓</div>
            <div className="success-text">Validation saved</div>
            <button className="btn" onClick={onReset}>Validate another</button>
        </div>
    )
}
