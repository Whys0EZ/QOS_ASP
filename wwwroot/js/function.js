    // Random color generator
    function randomRGBA(alpha = 0.7) {
        const r = Math.floor(Math.random() * 256);
        const g = Math.floor(Math.random() * 256);
        const b = Math.floor(Math.random() * 256);
        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }
    function generateColors(count) {
        return Array.from({ length: count }, (_, i) => 
            `hsl(${(i * 360 / count)}, 80%, 60%)`
        );
    }