/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./src/**/*.{js,jsx,ts,tsx}",
    ],
    theme: {
        extend: {
            colors: {
                // MindNest配色方案（基于UI截图分析）
                'cream': '#F5F5F0',           // 奶油白背景
                'sage-green': '#A8B5A5',      // 灰绿色（主色调）
                'blue-gray': '#8B9DAF',       // 蓝灰色按钮
                'pink-red': '#E8A5A5',        // 粉色删除按钮
            },
            borderRadius: {
                '3xl': '24px',
                '4xl': '32px',
            },
            fontFamily: {
                sans: ['Inter', 'system-ui', 'sans-serif'],
            },
            boxShadow: {
                'soft': '0 2px 8px rgba(0, 0, 0, 0.04)',
            },
            animation: {
                'fade-in': 'fadeIn 0.2s ease-in-out',
                'slide-up': 'slideUp 0.3s ease-out',
            },
            keyframes: {
                fadeIn: {
                    '0%': { opacity: '0' },
                    '100%': { opacity: '1' },
                },
                slideUp: {
                    '0%': { transform: 'translateY(20px)', opacity: '0' },
                    '100%': { transform: 'translateY(0)', opacity: '1' },
                }
            }
        },
    },
    plugins: [],
}
