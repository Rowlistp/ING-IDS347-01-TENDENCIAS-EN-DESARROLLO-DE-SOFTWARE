---
trigger: always_on
description: UI/UX Pro Max design intelligence for professional UI/UX design, styles, palettes, fonts, and accessibility.
---

## UI/UX Pro Max

When designing, refactoring, building, or reviewing UI/UX components (web, mobile, dashboards, landing pages, forms, modals):

1. **Design System & Intelligence:**
   - Run the design system search:
     python .agents/skills/ui-ux-pro-max/scripts/search.py <product_type> <industry> <keywords> --design-system
   - Use the retrieved design patterns, font pairings, and color palettes.

2. **Quality & Accessibility Checklist (Pre-delivery):**
   - **Icons:** No raw emojis for UI controls. Always use clean SVGs (Heroicons, Lucide, Feather).
   - **Clickable targets:** Minimum 44x44px for touch / mobile targets with cursor-pointer.
   - **Color Contrast:** Minimum 4.5:1 text-to-background contrast (WCAG AA).
   - **Transitions:** Smooth hover and active states (150ms-300ms, `transition-all` / `transition-colors`).
   - **Keyboard Navigation:** Explicit visible focus rings (`focus:outline-none focus:ring-2`).
   - **Responsiveness:** Fluid adaptation across mobile (<768px), tablet (768px-1024px), and desktop (>=1024px).
   - **Anti-patterns:** Avoid decorative clutter without filtering, avoid unstyled native form elements.
