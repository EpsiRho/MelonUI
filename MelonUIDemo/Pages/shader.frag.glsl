#version 330 core

#ifdef GL_ES
precision mediump float;
#endif

out vec4 FragColor;

uniform float time;
uniform vec2 resolution;

// Function to generate digital rainbow colors based on a value
vec3 digitalRainbow(float value) {
    float r = clamp(sin(value + 0.0) * 0.5 + 0.5, 0.0, 1.0);
    float g = clamp(sin(value + 2.0944) * 0.5 + 0.5, 0.0, 1.0);
    float b = clamp(sin(value + 4.18879) * 0.5 + 0.5, 0.0, 1.0);
    return vec3(r, g, b);
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy; // Normalize coordinates
    
    // Introduce random factors to add complexity
    float randomFactor = fract(sin(dot(uv, vec2(5555555.9898, 9.233))) * 33758.5453);
    
    // Calculate flow field direction based on uv coordinates and time
    float scale = 9.0;
    float angle = sin(uv.x * scale) + cos(uv.y * scale) + time;
    vec2 flowDir = vec2(cos(angle), sin(angle));
    
    // Apply flow field and random factors to uv coordinates
    vec2 newUV = uv + flowDir * randomFactor * 0.05;
    
    // Create brutalist-style pixel art pattern
    vec2 scaledUV = newUV * 20.0; // Scale uv coordinates
    float pixelValue = 1.0 - step(0.5, mod(scaledUV.x + scaledUV.y, 2.0)); // Inverted pattern
    
    // Background color (black)
    vec3 backgroundColor = vec3(0.0);
    
    // Digital rainbow colors for the white sections
    vec3 rainbowColor = digitalRainbow(mod(time + scaledUV.x + scaledUV.y, 6.2831)); // Modulate with time for animation
    
    // Combine colors based on pixelValue
    vec3 finalColor = mix(backgroundColor, rainbowColor, pixelValue);
    
    FragColor = vec4(finalColor, 1.0); // Set fragment color
}