import { useState, useEffect, useRef, useCallback, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import * as d3 from 'd3'
import { api } from '../api/dashboard.js'
import '../Atlas.css'
import { NOISE_COLOR, buildPalette } from '../constants/atlas.js'
import { positionLabels, runInitSim, runSpreadSim, updateBubbleSize } from '../features/atlas/atlasD3.js'
import { AtlasToolbar } from '../features/atlas/AtlasToolbar.jsx'
import { ClusterSidebar } from '../features/atlas/ClusterSidebar.jsx'

export default function Atlas() {
    const navigate = useNavigate()
    const svgRef = useRef(null)
    const tooltipRef = useRef(null)

    const [selectedRunId, setSelectedRunId] = useState(null)
    const [showNoise, setShowNoise] = useState(false)
    const [hoveredClusterId, setHoveredClusterId] = useState(null)
    const [pinnedClusterIds, setPinnedClusterIds] = useState(() => new Set())
    const [hideUnpinned, setHideUnpinned] = useState(false)
    const [highlightOnly, setHighlightOnly] = useState(false)
    const [viewMode, setViewMode] = useState('scatter')
    const [minConfidence, setMinConfidence] = useState(0.50)

    const zoomRef = useRef(null)
    const svgSelectionRef = useRef(null)
    const forceNodesRef = useRef([])
    const labelDataRef = useRef([])
    const pinHandlerRef = useRef(null)

    const { data: runs } = useQuery({ queryKey: ['runs'], queryFn: api.getRuns })
    const { data: atlas, isLoading, isError } = useQuery({
        queryKey: ['atlas', selectedRunId],
        queryFn: () => api.getAtlas(selectedRunId),
    })

    const clusterIds = useMemo(() => {
        if (!atlas?.points) return []
        return [...new Set(atlas.points.map(p => p.clusterId))]
            .filter(id => id !== -1)
            .sort((a, b) => a - b)
    }, [atlas])

    const colorMap = useMemo(() => {
        const palette = buildPalette(clusterIds.length)
        const map = new Map()
        clusterIds.forEach((id, i) => map.set(id, palette[i]))
        map.set(-1, NOISE_COLOR)
        return map
    }, [clusterIds])

    const activeClusterIds = useMemo(() => {
        if (pinnedClusterIds.size > 0) return pinnedClusterIds
        if (hoveredClusterId !== null) return new Set([hoveredClusterId])
        return null
    }, [pinnedClusterIds, hoveredClusterId])

    const visiblePoints = useMemo(() => {
        if (!atlas?.points?.length) return null
        const raw = atlas.points
        const points = showNoise ? raw : raw.filter(p => p.clusterId !== -1)
        const geoms = clusterIds.map(cid => {
            const pts = points.filter(p => p.clusterId === cid)
            return pts.length ? { clusterId: cid, points: pts, color: colorMap.get(cid) } : null
        }).filter(Boolean)
        return { geoms, points }
    }, [atlas, showNoise, clusterIds, colorMap])

    const handlePointClick = useCallback((node) => {
        if (node.clusterId === -1 || node.clusterId === undefined) return
        navigate(`/explorer?clusterId=${node.clusterId}&runId=${atlas.clusterRunId}`)
    }, [navigate, atlas])

    const togglePin = useCallback((clusterId) => {
        setHoveredClusterId(null)
        setPinnedClusterIds(prev => {
            const next = new Set(prev)
            next.has(clusterId) ? next.delete(clusterId) : next.add(clusterId)
            return next
        })
    }, [])

    const clearPin = useCallback(() => {
        setHoveredClusterId(null)
        setPinnedClusterIds(new Set())
        setHideUnpinned(false)
    }, [])

    // Keep D3 event handlers current without re-running structural setup
    useEffect(() => { pinHandlerRef.current = togglePin })

    // Spread pinned points apart, or snap all points back to rest positions
    useEffect(() => {
        if (!svgSelectionRef.current) return
        const circles = svgSelectionRef.current.selectAll('circle.point-node')

        if (pinnedClusterIds.size === 0 || highlightOnly) {
            circles.transition('pos').duration(300).ease(d3.easeCubicOut)
                .attr('cx', d => d.targetX)
                .attr('cy', d => d.targetY)
            return
        }

        const pinnedNodes = forceNodesRef.current.filter(n => pinnedClusterIds.has(n.clusterId))
        runSpreadSim(pinnedNodes, labelDataRef.current)

        circles.transition('pos').duration(400).ease(d3.easeCubicOut)
            .attr('cx', d => pinnedClusterIds.has(d.clusterId) ? d.x : d.targetX)
            .attr('cy', d => pinnedClusterIds.has(d.clusterId) ? d.y : d.targetY)
    }, [pinnedClusterIds, highlightOnly])

    // Build SVG structure: scales, layers, nodes, bubbles, labels, point circles
    useEffect(() => {
        if (!visiblePoints || !svgRef.current) return
        const { geoms, points } = visiblePoints

        const container = svgRef.current.parentElement
        const width = container.clientWidth || 800
        const height = container.clientHeight || 600
        const padding = 40

        d3.select(svgRef.current).selectAll('*').remove()
        const svg = d3.select(svgRef.current).attr('width', width).attr('height', height)
        svgSelectionRef.current = svg

        const bubbleLayer = svg.append('g').attr('class', 'bubbles-layer')
        const pointsLayer = svg.append('g').attr('class', 'points-layer')
        const labelsLayer = svg.append('g').attr('class', 'labels-layer')
        const xAxisG = svg.append('g').attr('class', 'axis x-axis').attr('transform', `translate(0,${height - padding})`)
        const yAxisG = svg.append('g').attr('class', 'axis y-axis').attr('transform', `translate(${padding},0)`)

        const xExt = d3.extent(points, p => p.x), xR = (xExt[1] - xExt[0]) || 1
        const yExt = d3.extent(points, p => p.y), yR = (yExt[1] - yExt[0]) || 1
        const xScale = d3.scaleLinear().domain([xExt[0] - xR * 0.05, xExt[1] + xR * 0.05]).range([padding, width - padding])
        const yScale = d3.scaleLinear().domain([yExt[0] - yR * 0.05, yExt[1] + yR * 0.05]).range([height - padding, padding])

        const drawAxes = (xs, ys) => {
            xAxisG.call(d3.axisBottom(xs).ticks(6).tickSize(-height + padding * 2))
            yAxisG.call(d3.axisLeft(ys).ticks(6).tickSize(-width + padding * 2))
            svg.selectAll('.axis line').attr('stroke', '#2a2a2a')
            svg.selectAll('.axis path').attr('stroke', '#3a3a3a')
            svg.selectAll('.axis text').attr('fill', '#666').attr('font-size', '11px')
        }
        drawAxes(xScale, yScale)

        const zoom = d3.zoom()
            .scaleExtent([0.05, 40])
            .on('zoom', ({ transform }) => {
                pointsLayer.attr('transform', transform)
                bubbleLayer.attr('transform', transform)
                labelsLayer.attr('transform', transform)
                drawAxes(transform.rescaleX(xScale), transform.rescaleY(yScale))
            })
        zoomRef.current = zoom
        svg.call(zoom)
        svg.on('dblclick.zoom', () => svg.transition().duration(300).call(zoom.transform, d3.zoomIdentity))

        const nodes = points.map(p => ({
            ...p,
            radius: p.clusterId === -1 ? 2.5 : 4.5,
            targetX: xScale(p.x),
            targetY: yScale(p.y),
        }))
        forceNodesRef.current = nodes
        runInitSim(nodes)

        const tooltip = d3.select(tooltipRef.current)
        const labelData = []

        geoms.forEach(g => {
            const clusterNodes = nodes.filter(n => n.clusterId === g.clusterId)
            const cx = d3.mean(clusterNodes, n => n.targetX)
            const cy = d3.mean(clusterNodes, n => n.targetY)

            const bubbleGroup = bubbleLayer.append('g')
                .attr('class', `bubble-group-${g.clusterId}`)
                .datum({ ...g, points: clusterNodes })
            bubbleGroup.append('circle').attr('class', 'outer-bubble-ring')
                .attr('cx', cx).attr('cy', cy).attr('fill', g.color).attr('opacity', 0.12)
            bubbleGroup.append('circle').attr('class', 'inner-bubble-dash')
                .attr('cx', cx).attr('cy', cy).attr('fill', 'none')
                .attr('stroke', g.color).attr('stroke-width', 1.2)
                .attr('stroke-dasharray', '4,4').attr('opacity', 0.35)

            const labelGroup = labelsLayer.append('g')
                .attr('class', `cluster-label-${g.clusterId}`)
                .style('cursor', 'pointer')
                .on('click', (event) => { event.stopPropagation(); pinHandlerRef.current?.(g.clusterId) })

            const bg = labelGroup.append('rect')
                .attr('fill', '#0f0f0f').attr('stroke', g.color).attr('stroke-width', 1)
                .attr('rx', 4).attr('opacity', 0.75).style('pointer-events', 'none')

            const text = labelGroup.append('text')
                .attr('text-anchor', 'middle').attr('dominant-baseline', 'middle')
                .attr('fill', '#fff').attr('font-size', '11px').attr('font-weight', '600')
                .text(`Cluster ${g.clusterId}`)

            labelData.push({ group: labelGroup, bg, text, allPoints: clusterNodes, clusterId: g.clusterId })
        })

        labelDataRef.current = labelData
        positionLabels(labelData, 0, nodes)

        pointsLayer.selectAll('circle.point-node')
            .data(nodes, d => d.id || `${d.x}-${d.y}`)
            .join('circle')
            .attr('class', 'point-node')
            .attr('cx', d => d.targetX).attr('cy', d => d.targetY)
            .attr('r', d => d.radius)
            .attr('fill', d => colorMap.get(d.clusterId))
            .attr('stroke', 'none')
            .attr('cursor', d => d.clusterId === -1 ? 'default' : 'pointer')
            .on('mouseenter', (event, d) => {
                if (d.clusterId === -1) return
                d3.select(event.currentTarget).attr('r', d.radius + 1.5)
                tooltip.style('display', 'block')
                    .style('left', `${event.offsetX + 12}px`).style('top', `${event.offsetY - 8}px`)
                    .html(`<div style="font-weight:600;margin-bottom:4px;color:#fff;">${d.problemSummary || 'No summary'}</div>
                           <div style="font-size:11px;color:#888;">Cluster ${d.clusterId} · ${(d.confidence * 100).toFixed(0)}% confidence</div>`)
            })
            .on('mousemove', (event) => {
                tooltip.style('left', `${event.offsetX + 12}px`).style('top', `${event.offsetY - 8}px`)
            })
            .on('mouseleave', (event, d) => {
                tooltip.style('display', 'none')
                d3.select(event.currentTarget).attr('r', d.radius)
            })
            .on('click', (_, d) => handlePointClick(d))

        const currentSvgNode = svgRef.current
        return () => { d3.select(currentSvgNode).selectAll('*').remove() }
    }, [visiblePoints, handlePointClick, colorMap])

    // Update point/label/bubble visibility and opacity when selection or filters change
    useEffect(() => {
        if (!svgSelectionRef.current) return
        const svg = svgSelectionRef.current
        const points = svg.selectAll('circle.point-node')
        const passes = d => d.clusterId === -1 || d.confidence >= minConfidence

        svg.select('.bubbles-layer').attr('display', viewMode === 'bubble' ? 'inherit' : 'none')

        if (activeClusterIds === null) {
            points.attr('display', d => passes(d) ? 'inherit' : 'none')
                .transition('style').duration(150)
                .attr('opacity', d => d.clusterId === -1 ? 0.3 : 0.85)
        } else {
            points.attr('display', d => {
                if (!passes(d)) return 'none'
                if (hideUnpinned && !activeClusterIds.has(d.clusterId)) return 'none'
                return 'inherit'
            })
            points.transition('style').duration(150)
                .attr('opacity', d => activeClusterIds.has(d.clusterId) ? 1.0 : 0.1)
        }

        clusterIds.forEach(cid => {
            const isActive = activeClusterIds === null || activeClusterIds.has(cid)
            const hidden = hideUnpinned && !isActive

            svg.select(`.cluster-label-${cid}`)
                .attr('display', hidden ? 'none' : 'inherit')
                .transition('style').duration(150).attr('opacity', isActive ? 1 : 0.2)

            if (viewMode === 'bubble') {
                const group = svg.select(`.bubble-group-${cid}`)
                if (group.empty()) return
                group.attr('display', hidden ? 'none' : 'inherit')
                    .transition('style').duration(150).attr('opacity', isActive ? 1.0 : 0.1)
                if (isActive) {
                    const activePoints = group.datum().points.filter(n => n.confidence >= minConfidence)
                    updateBubbleSize(group, activePoints)
                }
            }
        })

        if (labelDataRef.current.length > 0) positionLabels(labelDataRef.current, minConfidence, forceNodesRef.current)
    }, [activeClusterIds, viewMode, minConfidence, hideUnpinned, clusterIds, visiblePoints])

    const hasPins = pinnedClusterIds.size > 0

    return (
        <div className="page">
            <AtlasToolbar
                viewMode={viewMode} setViewMode={setViewMode}
                minConfidence={minConfidence} setMinConfidence={setMinConfidence}
                hasPins={hasPins}
                highlightOnly={highlightOnly} setHighlightOnly={setHighlightOnly}
                hideUnpinned={hideUnpinned} setHideUnpinned={setHideUnpinned}
                pinnedClusterIds={pinnedClusterIds} clearPin={clearPin}
                showNoise={showNoise} setShowNoise={setShowNoise}
                selectedRunId={selectedRunId} setSelectedRunId={setSelectedRunId}
                runs={runs}
            />

            {isLoading && <div className="atlas-state">Loading...</div>}
            {isError && <div className="atlas-state error">Failed to load atlas.</div>}

            {!isLoading && !isError && (
                <div className="atlas-content">
                    <div className="plot-wrapper">
                        <svg ref={svgRef} />
                        <div ref={tooltipRef} className="atlas-tooltip" />
                    </div>
                    <ClusterSidebar
                        clusterIds={clusterIds}
                        pinnedClusterIds={pinnedClusterIds}
                        hoveredClusterId={hoveredClusterId}
                        colorMap={colorMap}
                        hasPins={hasPins}
                        togglePin={togglePin}
                        setHoveredClusterId={setHoveredClusterId}
                    />
                </div>
            )}

            {atlas && (
                <div className="atlas-footer">
                    {atlas.points.length} ideas · {clusterIds.length} clusters · click to pin (multiple) · scroll to zoom
                </div>
            )}
        </div>
    )
}
