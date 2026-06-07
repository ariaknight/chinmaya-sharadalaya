import { defineCollection, z } from 'astro:content';
import { glob } from 'astro/loaders';

const eventsCollection = defineCollection({
  loader: glob({ pattern: '**/*.md', base: './src/content/events' }),
  schema: z.object({
    title: z.string(),
    dayOfWeek: z.string(), // e.g., "Friday"
    time: z.string(), // e.g., "7:30 PM - 8:30 PM"
    location: z.string().default('CMSA - Sharadalaya'),
    instructor: z.string().optional(),
    order: z.number(), // For sorting Monday-Sunday
    description: z.string().optional(),
  }),
});

export const collections = {
  events: eventsCollection,
};
