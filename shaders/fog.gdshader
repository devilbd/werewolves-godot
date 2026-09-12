shader_type canvas_item;

uniform sampler2D noise_texture : repeat_enable, filter_linear;
uniform vec4 fog_color : source_color = vec4(0.88, 0.94, 1.0, 0.92);
uniform vec2 scroll_speed1 = vec2(0.018, 0.012);
uniform vec2 scroll_speed2 = vec2(-0.012, 0.016);
uniform float density : hint_range(0.0, 3.0) = 1.15;
uniform float coverage : hint_range(0.0, 1.0) = 0.58;
uniform float disappear_cycle_speed : hint_range(0.01, 1.0) = 0.08;
uniform float time_offset = 0.0;
uniform float radial_falloff : hint_range(0.0, 1.0) = 0.45;

void fragment() {
    // Distance from center of the control (0 at center, 1 at edge)
    vec2 centered_uv = (UV - vec2(0.5)) * 2.0;
    float dist = length(centered_uv);

    // Soft circular edge falloff so the fog zone smoothly blends into the surroundings
    float edge_fade = smoothstep(1.0, 1.0 - radial_falloff, dist);

    // Dual-layer UV scrolling for natural volumetric swirling
    float t = TIME + time_offset;
    vec2 uv1 = UV + t * scroll_speed1;
    vec2 uv2 = UV * 1.35 + t * scroll_speed2;

    float n1 = texture(noise_texture, uv1).r;
    float n2 = texture(noise_texture, uv2).r;

    // Combined multi-layer noise
    float combined = n1 * 0.58 + n2 * 0.42;

    // Dynamic appearance & disappearance over time
    // An undulating sine wave modulates the coverage threshold,
    // causing puffy cloud masses to form (appear), drift, and dissolve (disappear)
    float time_pulse = sin(t * disappear_cycle_speed) * 0.5 + 0.5;
    float dynamic_coverage = mix(coverage * 0.45, coverage * 1.35, time_pulse);

    // Threshold into soft puffy clouds with rich body
    float cloud = smoothstep(1.0 - dynamic_coverage, (1.0 - dynamic_coverage) + 0.28, combined);

    // Final alpha
    float alpha = cloud * edge_fade * density * fog_color.a;

    COLOR = vec4(fog_color.rgb, clamp(alpha, 0.0, 1.0));
}
