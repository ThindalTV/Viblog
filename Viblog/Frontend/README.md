# Vilog Frontend

## Overview

The frontend module provides a clean, modern blog UI inspired by professional developer blogs. It features a statically-rendered public interface with a minimalist design.

## Structure

### Layout
- **BlogLayout.razor** - Main layout for the blog with header, content area, and footer

### Pages
- **Index.razor** - Home page featuring:
  - Hero section with profile image and bio
  - Social media links
  - Blog post listing area (placeholder for future dynamic content)
  - About section

### Styling
- **blog.css** - Custom CSS for the blog design featuring:
  - Clean, professional typography
  - Responsive grid layout
  - Smooth hover transitions
  - Mobile-first responsive design
  - Modern color scheme (dark header/footer, light content area)

### Assets
- **Profile Image** - Located at `/img/profile.png` (placeholder SVG)
- **Social Icons** - Located at `/img/logos/`:
  - `linkedin.svg`
  - `github.svg`
  - `twitter.svg`

## Customization

### Updating Personal Information

Edit `Frontend/Pages/Index.razor`:
- Change "Your Name" to your actual name
- Update the bio text
- Modify social media links

Edit `Frontend/Layout/BlogLayout.razor`:
- Update header title and tagline
- Customize footer text

### Styling

The blog uses custom CSS in `wwwroot/blog.css`. Key variables to customize:
- Colors: Header background (`#1a1a1a`), text, borders
- Spacing: Padding, margins, gaps
- Typography: Font sizes, weights, families

### Adding Real Content

The current implementation includes placeholder content. To add real blog posts:
1. Create blog post models in the data layer
2. Create repository/service classes following the Display-Facade-Repository pattern
3. Update `Index.razor` to fetch and display actual posts
4. Add pagination and search features as needed

## Next Steps

- Implement blog post listing with database integration
- Add search functionality
- Create blog post detail pages
- Add categories and tags
- Implement markdown rendering for posts
- Add media embedding support (images, videos)

## Technical Notes

- Uses Blazor static rendering for optimal performance
- Mobile-responsive design with breakpoint at 768px
- Follows project conventions for minimal logic in views
- Ready for integration with CosmosDB backend
- Prepared for Telerik UI component integration
