# Chinmaya Sharadalaya — Website

This repository contains the Astro-based website for Chinmaya Sharadalaya. It was created from the Astro minimal starter and contains the site's source in `src/` and static assets in `public/`.

## Quick Start

Install dependencies and start the dev server:

```sh
npm install
npm run dev
```

Build for production and preview the build:

```sh
npm run build
npm run preview
```

Build output is written to `dist/`.

## Project Layout

- `src/` — site source (pages, layouts, components)
- `public/` — static assets (images, favicon, etc.)
- `dist/` — generated build output (ignored by git)

## Notes

- This project uses Astro. See the Astro docs for advanced usage: https://docs.astro.build
- Common files/directories to ignore are added to `.gitignore`: `dist/`, `.astro/`, `.playwright/`, and macOS `.DS_Store`.

If you'd like, I can add a short deploy section for your chosen hosting provider or include contributor/setup notes.

## Adding and Editing Pages

- Pages live in the `src/pages/` directory. Each file becomes a route based on its file path and name:
	- `src/pages/index.astro` -> `/`
	- `src/pages/about.astro` -> `/about`
	- `src/pages/docs/getting-started.astro` -> `/docs/getting-started`
- Supported page formats: `.astro`, `.md`, and `.mdx`. Use frontmatter at the top of Markdown pages for metadata.
- To add a new page: create a new file under `src/pages/`, then visit the corresponding route while the dev server is running.

Example — add a basic page:

```astro
---
title: "New Page"
---
<h1>{title}</h1>
<p>Page content goes here.</p>
```

- To change an existing page, edit the file in `src/pages/` or a shared layout/component in `src/layouts/` or `src/components/`. The dev server reloads automatically.
- If you add assets (images), put them in `public/` and reference them with absolute paths (for example `/images/photo.jpg`).

Example — reuse `Layout.astro` and components

You can reuse `Layout.astro` and components by importing them into a page. An example page has been added at `src/pages/example.astro`.

```astro
---
title: "Example Page"
---
<script setup>
import Layout from '../layouts/Layout.astro';
import Hero from '../components/Hero.astro';
</script>

<Layout>
	<Hero />
	<main class="max-w-3xl mx-auto p-8">
		<h1>{title}</h1>
		<p>This page shows how to use `Layout.astro` and components from `src/components/`.</p>
	</main>
</Layout>
```
