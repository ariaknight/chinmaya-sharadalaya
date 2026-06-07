# Chinmaya Mission San Antonio - Development Guide

**Project**: Astro static site generator with Tailwind CSS v4  
**Build Command**: `npm run build`  
**Dev Server**: `npm run dev`  
**Output**: `/dist/` directory (7 pages)

---

## Project Structure

```
src/
  components/
    Hero.astro           # Main hero template (index page)
    Header.astro         # Navigation header
    Footer.astro         # Footer with contact info
    CSPSection.astro     # Ceremonies & Sacred Programs section
    JnanashaktiSection.astro  # Jnanashakti section
    EventGrid.astro      # Events grid display
    Layout.astro         # Main layout wrapper
  
  pages/
    index.astro          # Home page (uses Hero component)
    about.astro          # About Us page
    temple.astro         # Sharadamba Temple page
    parivar.astro        # Chinmaya Parivar membership page
    jnanashakti.astro    # Jnanashakti sponsorship page
    donate.astro         # Donations page
    csp.astro            # Ceremonies & Sacred Programs page
    schedule.astro       # Weekly schedule page (referenced in buttons)
  
  styles/
    global.css           # Global styles and Tailwind imports
  
  layouts/
    Layout.astro         # Main layout component
```

---

## Color System

### Custom Extended Tailwind Colors
- **Maroon**: Primary color (#8B2F39) with 50-950 spectrum
- **Burgundy**: Secondary color (#A0373D) with 50-950 spectrum

### Important: Use Arbitrary Hex Values
Due to Tailwind v4 content scanner limitations, **always use hex values for guaranteed rendering**:

✅ **CORRECT**: `bg-[#8B2F39]` `text-[#f5f1ef]` `border-[#6B2027]`  
❌ **AVOID**: `bg-maroon-500` `text-burgundy-100` `border-maroon-700`

### Typography
- **Headings**: Cormorant Garamond (serif) - font-serif
- **Body**: Inter (sans-serif) - default
- **Title Classes**: `text-5xl md:text-6xl lg:text-7xl font-serif font-bold`

---

## Hero Section Template

All pages use the same hero structure. **Reference**: `src/components/Hero.astro`

### Key Elements
1. **Background**: Full-screen hero with image + gradient overlay
2. **Structure**: `section.relative.h-screen.flex.items-center.justify-center`
3. **Content**: Centered with `max-w-4xl mx-auto px-4`
4. **Animation**: Fade-in effect (0.8s ease-in)

### Code Pattern
```astro
<section class="relative h-screen flex items-center justify-center overflow-hidden">
  <!-- Background Image -->
  <div
    class="absolute inset-0 bg-cover bg-center bg-no-repeat"
    style="background-image: url('https://images.unsplash.com/...');"
  ></div>

  <!-- Dark Gradient Overlay -->
  <div class="absolute inset-0 bg-gradient-to-r from-maroon-900/80 via-burgundy-900/70 to-maroon-900/80"></div>

  <!-- Hero Content -->
  <div class="relative z-10 text-center max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
    <!-- Decorative Top Element -->
    <div class="mb-6 flex justify-center">
      <div class="h-1 w-20 bg-gradient-to-r from-[#8B2F39] to-[#6B2027] rounded-full"></div>
    </div>

    <!-- Title -->
    <h1 class="text-5xl md:text-6xl lg:text-7xl font-serif font-bold text-white mb-6 leading-tight tracking-wide">
      Page Title
    </h1>

    <!-- Mission/Tagline Statement -->
    <p class="text-xl md:text-2xl lg:text-3xl text-maroon-100 font-light mb-8 italic leading-relaxed max-w-3xl mx-auto">
      "Your tagline or mission here."
    </p>

    <!-- Subtitle -->
    <p class="text-lg md:text-xl text-maroon-100 mb-12 font-light max-w-2xl mx-auto">
      Descriptive text about the page.
    </p>

    <!-- Scroll Down Indicator -->
    <div class="mt-16 flex justify-center">
      <div class="animate-bounce">
        <svg class="w-6 h-6 text-[#8B2F39]" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M19 14l-7 7m0 0l-7-7m7 7V3"></path>
        </svg>
      </div>
    </div>
  </div>

  <!-- Decorative Bottom Curve -->
  <div class="absolute bottom-0 left-0 right-0 h-32 bg-gradient-to-t from-white to-transparent pointer-events-none"></div>
</section>

<style>
  section {
    animation: fadeIn 0.8s ease-in;
  }

  @keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
  }
</style>
```

---

## Common Tasks

### 1. Update Hero Section Text
Edit the page file (e.g., `src/pages/about.astro`):
- Change the `<h1>` text for page title
- Update the tagline/mission statement in the italic `<p>`
- Update the descriptive subtitle

### 2. Change Background Image
Replace the URL in the `style` attribute of the Background Image div:
```astro
style="background-image: url('YOUR_NEW_IMAGE_URL');"
```

### 3. Add/Modify Buttons
Use consistent styling with hex values:
```astro
<a
  href="/path"
  class="px-8 py-4 bg-[#8B2F39] hover:bg-[#6B2027] text-white font-semibold rounded-lg shadow-lg transition-all duration-300 transform hover:scale-105 active:scale-95 text-lg"
>
  Button Text
</a>
```

### 4. Fix White-on-Light Contrast Issues
Add global CSS rules in `src/styles/global.css`:
```css
a[class*="bg-maroon-50"][class*="text-white"] {
  color: var(--text-dark) !important;
  background-color: var(--secondary-light) !important;
}
```

### 5. Modify Header Navigation
Edit `src/components/Header.astro`:
- Current order: Home, Temple, Parivar, Jnanashakti, About Us
- Contact link removed (was requested by design)
- Sticky header with transparency

### 6. Update Footer
Edit `src/components/Footer.astro`:
- 3-column layout: Mission, Contact Us, Follow Us
- Update contact info, social links
- Dynamic copyright year included

---

## Color Hex Reference

| Element | Hex Value | Tailwind Equivalent |
|---------|-----------|-------------------|
| Primary Maroon | #8B2F39 | maroon-500 |
| Maroon 900 | #6B2027 | maroon-900 |
| Maroon Light | #f5f1ef | maroon-50 |
| Burgundy 900 | #481719 | burgundy-900 |
| Text on maroon | #8B2F39 | maroon-100 |
| Light overlay | #faf5f4 | maroon-50 |

---

## Build & Deployment

### Local Development
```bash
npm run dev
# Opens dev server (usually http://localhost:4321)
```

### Production Build
```bash
npm run build
# Generates optimized static files in /dist/
# Typical build time: 1.0-1.7 seconds
# Output: 7 HTML pages + assets
```

### Validation
After any changes:
1. Run `npm run build` to ensure no errors
2. Check `/dist/` folder for all 7 pages
3. Build should complete with 0 errors

---

## Known Issues & Workarounds

### Issue 1: Tailwind Utilities Not Rendering
**Problem**: Classes like `bg-maroon-700` or `text-burgundy-100` don't generate CSS  
**Cause**: Tailwind v4 content scanner sometimes misses color utility classes  
**Solution**: Use arbitrary hex values instead
```css
/* ❌ Won't render */
class="bg-maroon-700"

/* ✅ Will render */
class="bg-[#8B2F39]"
```

### Issue 2: White Text on Light Backgrounds
**Problem**: Text becomes invisible on light-colored backgrounds  
**Cause**: Color utilities not applying correctly  
**Solution**: Add global CSS with attribute selectors and `!important`
```css
[class*="bg-maroon-50"][class*="text-white"] {
  color: var(--text-dark) !important;
  background-color: var(--secondary-light) !important;
}
```

### Issue 3: Opacity Variants Not Working
**Problem**: Classes like `bg-maroon-50/30` not generating  
**Solution**: Use hex with opacity: `bg-[#8B2F39]/30`

---

## Page Files Overview

### Index (index.astro)
- Uses `<Hero />` component directly
- Includes: CSPSection, JnanashaktiSection, EventGrid
- Hero has background image + fade animation

### About (about.astro)
- Full hero with background image
- Content sections with mission, history, values
- 3-column stats grid

### Temple (temple.astro)
- Hero section with background image
- Temple history and presiding deities
- Puja schedules and rituals

### Parivar (parivar.astro)
- Membership information page
- Programs & services overview
- Membership benefits

### Jnanashakti (jnanashakti.astro)
- Sponsorship campaign page
- Sponsorship tiers (Bhakti, Shakti, Sannidhi, Grand Patron)
- Donation tracking cards

### Donate (donate.astro)
- General donations page
- Quick donation options
- Impact messaging

### CSP (csp.astro)
- Chinmaya Satyanarayana Pooja info
- Registration form
- Ceremony details

---

## CSS Variables (global.css)

```css
--primary-maroon: #8B2F39
--secondary-burgundy: #A0373D
--secondary-light: #faf5f4
--text-dark: #220F0D
--text-maroon: #6B2027
```

---

## Common Modifications Checklist

- [ ] Update hero title and text
- [ ] Change background image URL (if needed)
- [ ] Verify hex color values (not Tailwind classes)
- [ ] Test button contrast and hover states
- [ ] Run `npm run build` to validate
- [ ] Check that all 7 pages compile with 0 errors
- [ ] Test on mobile (responsive breakpoints: sm, md, lg)

---

## Quick Reference: Hex Color Palette

```
Maroons:        Burgundies:      Neutrals:
#8B2F39 (main)  #A0373D (main)   #f5f1ef (light)
#6B2027 (dark)  #481719 (dark)   #faf5f4 (very light)
#e0c3ba (light) 
#d5afa3 (accent)
```

---

**Last Updated**: June 7, 2026  
**Framework**: Astro + Tailwind CSS v4  
**Status**: All 7 pages complete with standardized hero system
