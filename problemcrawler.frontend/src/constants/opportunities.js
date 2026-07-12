export const SCORE_FIELDS = [
    { key: 'painIntensityScore',     label: 'Pain',      color: '#e05555', description: 'How intensely users feel this problem' },
    { key: 'mentionFrequencyScore',  label: 'Frequency', color: '#e09a55', description: 'How often it is mentioned across threads' },
    { key: 'solutionVacuumScore',    label: 'Vacuum',    color: '#a855e0', description: 'Lack of existing workarounds or solutions' },
    { key: 'softwareMarketScore',    label: 'Market',    color: '#55a8e0', description: 'How well-suited this is for a software product' },
    { key: 'authorBreadthScore',     label: 'Authors',   color: '#55e09a', description: 'How many distinct people reported this problem' },
]

export const SORT_OPTIONS = [
    { value: 'opportunityScore',      label: 'Opportunity Score' },
    { value: 'painIntensityScore',    label: 'Pain' },
    { value: 'mentionFrequencyScore', label: 'Frequency' },
    { value: 'solutionVacuumScore',   label: 'Solution Vacuum' },
    { value: 'softwareMarketScore',   label: 'Market' },
    { value: 'authorBreadthScore',    label: 'Authors' },
    { value: 'size',                  label: 'Cluster Size' },
]
