export const NOISE_COLOR = '#444'

// Generate a large, well-spread palette: golden-angle hue stepping keeps consecutive
// colors far apart on the wheel, while cycling saturation/lightness adds variation.
export function buildPalette(count) {
    const sats = [70, 58, 82]
    const lights = [62, 52, 70]
    const colors = []
    let hue = 0
    for (let i = 0; i < count; i++) {
        hue = (hue + 137.508) % 360
        colors.push(hslToHex(hue, sats[i % sats.length], lights[i % lights.length]))
    }
    return colors
}

function hslToHex(h, s, l) {
    s /= 100; l /= 100
    const k = n => (n + h / 30) % 12
    const a = s * Math.min(l, 1 - l)
    const f = n => {
        const c = l - a * Math.max(-1, Math.min(k(n) - 3, 9 - k(n), 1))
        return Math.round(255 * c).toString(16).padStart(2, '0')
    }
    return `#${f(0)}${f(8)}${f(4)}`
}
