import * as d3 from 'd3'

export function positionLabels(labelData, minConf, allNodes) {
    const HALF_H = 10
    labelData.forEach(l => {
        const confPts = l.allPoints.filter(n => n.confidence >= minConf)
        const pts = confPts.length > 0 ? confPts : l.allPoints
        l.posX = pts.length === 1
            ? pts[0].targetX
            : (d3.min(pts, n => n.targetX) + d3.max(pts, n => n.targetX)) / 2
        l.posY = d3.min(pts, n => n.targetY) - 24
        l.halfW = (l.text.node().getBBox().width + 12) / 2
        l.w = l.halfW * 2
    })

    // Iteratively push labels out of data points and each other along the axis of
    // least overlap, so they settle around their clusters instead of piling upward.
    const collidePoints = (allNodes ?? []).filter(n => n.clusterId !== -1 && n.confidence >= minConf)
    for (let it = 0; it < 60; it++) {
        labelData.forEach(l => {
            for (const n of collidePoints) {
                const ox = l.halfW + n.radius + 1 - Math.abs(n.targetX - l.posX)
                const oy = HALF_H + n.radius + 1 - Math.abs(n.targetY - l.posY)
                if (ox <= 0 || oy <= 0) continue
                if (oy <= ox) l.posY += l.posY <= n.targetY ? -oy : oy
                else l.posX += l.posX <= n.targetX ? -ox : ox
            }
        })
        for (let i = 0; i < labelData.length; i++) {
            for (let j = i + 1; j < labelData.length; j++) {
                const a = labelData[i], b = labelData[j]
                const ox = a.halfW + b.halfW + 2 - Math.abs(a.posX - b.posX)
                const oy = 20 - Math.abs(a.posY - b.posY)
                if (ox <= 0 || oy <= 0) continue
                if (oy <= ox) {
                    const s = oy / 2
                    a.posY <= b.posY ? (a.posY -= s, b.posY += s) : (a.posY += s, b.posY -= s)
                } else {
                    const s = ox / 2
                    a.posX <= b.posX ? (a.posX -= s, b.posX += s) : (a.posX += s, b.posX -= s)
                }
            }
        }
    }

    labelData.forEach(l => {
        l.text.attr('x', l.posX).attr('y', l.posY)
        const bbox = l.text.node().getBBox()
        l.bg.attr('x', bbox.x - 6).attr('y', bbox.y - 3).attr('width', bbox.width + 12).attr('height', bbox.height + 6)
    })
}

// Spread nodes slightly from their UMAP positions to resolve initial overlaps
export function runInitSim(nodes) {
    nodes.forEach(n => { n.x = n.targetX; n.y = n.targetY; n.vx = 0; n.vy = 0 })
    const sim = d3.forceSimulation(nodes)
        .force('collide', d3.forceCollide(d => d.radius + 1.5).strength(1))
        .force('x', d3.forceX(d => d.targetX).strength(0.25))
        .force('y', d3.forceY(d => d.targetY).strength(0.25))
        .stop()
    for (let i = 0; i < 300; i++) sim.tick()
    // Bake results back into targetX/targetY so snap-back returns to these positions
    nodes.forEach(n => { n.targetX = n.x; n.targetY = n.y })
}

// Spread all pinned nodes apart for clickability and clear of their own cluster labels,
// while a strong restoring force keeps the cluster anchored near its original spot.
export function runSpreadSim(pinnedNodes, labelData) {
    const pinnedLabels = labelData.filter(l =>
        l.posY !== undefined && l.w !== undefined && pinnedNodes.some(n => n.clusterId === l.clusterId))
    pinnedNodes.forEach(n => { n.x = n.targetX; n.y = n.targetY; n.vx = 0; n.vy = 0 })
    const sim = d3.forceSimulation(pinnedNodes)
        .force('collide', d3.forceCollide(d => d.radius + 3).strength(1))
        .force('x', d3.forceX(d => d.targetX).strength(0.3))
        .force('y', d3.forceY(d => d.targetY).strength(0.3))
        .force('label-avoid', () => {
            pinnedLabels.forEach(l => {
                const halfW = l.w / 2 + 10
                const bottom = l.posY + 14
                pinnedNodes.forEach(n => {
                    if (Math.abs(n.x - l.posX) < halfW && n.y < bottom)
                        n.vy += (bottom - n.y) * 0.2
                })
            })
        })
        .stop()
    for (let i = 0; i < 300; i++) sim.tick()
}

// Resize a bubble group's rings to fit the given active points
export function updateBubbleSize(group, activePoints) {
    if (activePoints.length === 0) return
    const cx = d3.mean(activePoints, n => n.targetX)
    const cy = d3.mean(activePoints, n => n.targetY)
    const r = (d3.max(activePoints, n => Math.hypot(n.targetX - cx, n.targetY - cy)) ?? 0) + 15
    group.select('.outer-bubble-ring').attr('cx', cx).attr('cy', cy).attr('r', r)
    group.select('.inner-bubble-dash').attr('cx', cx).attr('cy', cy).attr('r', Math.max(r * 0.5, 10))
}
