# CSS Isolation Strategy

## Overview
This project uses **Blazor CSS Isolation** for component and page-specific styles, keeping the global `blog.scss` file minimal and focused only on truly global utilities and variables.

## Why CSS Isolation?

### Benefits
1. **Scoped Styles**: Styles only apply to their component/page
2. **No Naming Conflicts**: Each component gets unique CSS selectors
3. **Better Maintainability**: Styles live next to the component they style
4. **Automatic Cleanup**: Delete a component, its styles go with it
5. **Smaller Global CSS**: Reduces global namespace pollution
6. **Better Performance**: Browsers can optimize scoped CSS better

### Before (Global SCSS)
```scss
// blog.scss - Everything in one file
.blog-post { ... }
.post-title { ... }
.post-detail { ... }
.pagination { ... }
// 500+ lines of mixed styles
```

**Problems:**
- Hard to know which styles belong where
- Risk of selector conflicts
- Unused styles accumulate
- Changes affect entire app

### After (CSS Isolation)
```
Viblog/Frontend/
??? Components/
?   ??? PostCard.razor
?   ??? PostCard.razor.css          ? Component styles
??? Pages/
?   ??? Index.razor
?   ??? Index.razor.css             ? Page styles
?   ??? Posts.razor
?   ??? Posts.razor.css             ? Page styles
?   ??? Post.razor
?   ??? Post.razor.css              ? Page styles
??? Layout/
?   ??? BlogLayout.razor
?   ??? BlogLayout.razor.css        ? Layout styles
??? wwwroot/
    ??? blog.scss                   ? Global utilities only
    ??? styles/
        ??? _variables.scss         ? Shared variables
        ??? _mixins.scss            ? Shared mixins
```

## File Structure

### Component-Specific Styles
Each Razor component has a matching `.razor.css` file:

| Component/Page | CSS File |
|---------------|----------|
| `PostCard.razor` | `PostCard.razor.css` |
| `Post.razor` | `Post.razor.css` |
| `Posts.razor` | `Posts.razor.css` |
| `Index.razor` | `Index.razor.css` |
| `BlogLayout.razor` | `BlogLayout.razor.css` |

### Global Styles
**`blog.scss`** - Only contains:
- Global resets
- Utility classes (`.text-center`, `.mt-1`, etc.)
- No component-specific styles

**`_variables.scss`** - Shared variables:
- Colors
- Font families
- Spacing units
- Breakpoints

**`_mixins.scss`** - Shared mixins:
- Transitions
- Flexbox utilities
- Media queries

## How CSS Isolation Works

### Automatic Scoping
Blazor automatically scopes CSS by adding unique attributes:

```html
<!-- Before: Your HTML -->
<article class="blog-post">
    <h3 class="post-title">My Post</h3>
</article>

<!-- After: Blazor renders -->
<article class="blog-post" b-xyz123>
    <h3 class="post-title" b-xyz123>My Post</h3>
</article>
```

```css
/* Your CSS */
.blog-post { ... }
.post-title { ... }

/* Blazor generates */
.blog-post[b-xyz123] { ... }
.post-title[b-xyz123] { ... }
```

### Result
- Styles only apply to elements with the matching scope attribute
- No conflicts with other components using the same class names
- Each component is self-contained

## Isolated CSS Files

### PostCard.razor.css
**Contains:**
- `.blog-post` card container
- `.post-title` styles
- `.post-meta` metadata
- `.post-excerpt` excerpt text
- `.post-tags` and `.tag` styles
- `.read-more-link` styling
- Responsive styles for mobile

**Scope:** Only affects `PostCard` component instances

### Post.razor.css
**Contains:**
- `.post-detail` container
- `.post-header` header section
- `.post-content` rich content typography
  - Headings (h2, h3, h4)
  - Paragraphs, lists, blockquotes
  - Code blocks
  - Images and links
- `.post-footer` footer section
- `.post-navigation` navigation links
- Loading/error states
- Responsive styles

**Scope:** Only affects the `Post` detail page

### Posts.razor.css
**Contains:**
- `.blog-section` section container
- `.section-title` section headings
- `.pagination` pagination container
- `.pagination-controls` button group
- `.pagination-btn` button styles
- Responsive pagination

**Scope:** Only affects the `Posts` list page

### Index.razor.css
**Contains:**
- `.hero-section` hero container
- `.hero-content` grid layout
- `.profile-image-container` avatar
- `.social-links` social media links
- `.view-all-posts` call-to-action
- Responsive hero layout

**Scope:** Only affects the `Index` (home) page

### BlogLayout.razor.css
**Contains:**
- `.blog-container` main container
- `.blog-header` header section
- `.blog-title` and `.blog-tagline`
- `.blog-main` main content area
- `.blog-footer` footer section
- Responsive layout

**Scope:** Only affects pages using `BlogLayout`

## Global Utilities (blog.scss)

### When to Use Global CSS
Only for truly global, reusable utilities:
```scss
// ? Good - Reusable utility
.text-center { text-align: center; }
.mt-2 { margin-top: 1rem; }

// ? Bad - Component-specific
.blog-post { ... }  // Belongs in PostCard.razor.css
```

### Available Utilities
- **Spacing**: `.mt-0` through `.mt-4`, `.mb-0` through `.mb-4`
- **Text Alignment**: `.text-center`
- **Accessibility**: `.visually-hidden`

## Best Practices

### DO ?
- Put component-specific styles in `.razor.css` files
- Use simple class names (`.post-title` not `.Viblog-post-card-title`)
- Keep global utilities minimal
- Use CSS custom properties for theming if needed
- Leverage Blazor's automatic scoping

### DON'T ?
- Add component styles to `blog.scss`
- Use overly specific selectors (Blazor handles scoping)
- Create global styles for one-off use cases
- Duplicate styles across multiple `.razor.css` files
- Use `!important` to override scoped styles

## Adding New Components

### Step-by-Step
1. Create your component: `MyComponent.razor`
2. Create matching CSS file: `MyComponent.razor.css`
3. Write styles using simple class names
4. No need to worry about naming conflicts!

**Example:**
```razor
<!-- MyComponent.razor -->
<div class="container">
    <h2 class="title">Hello</h2>
</div>
```

```css
/* MyComponent.razor.css */
.container {
    padding: 1rem;
    background: white;
}

.title {
    font-size: 2rem;
    color: #333;
}
```

## Sharing Styles Between Components

### Option 1: Global Utility (if truly reusable)
```scss
// blog.scss
.btn {
    padding: 0.5rem 1rem;
    border: none;
    cursor: pointer;
}
```

### Option 2: Shared Component
```razor
<!-- Button.razor -->
<button class="btn">@ChildContent</button>

<!-- Button.razor.css -->
.btn {
    padding: 0.5rem 1rem;
    border: none;
    cursor: pointer;
}
```

### Option 3: CSS Variables (for theming)
```scss
// _variables.scss
:root {
    --color-primary: #1a1a1a;
    --spacing-md: 1rem;
}

// Any .razor.css file
.my-element {
    color: var(--color-primary);
    padding: var(--spacing-md);
}
```

## Responsive Design

### In Isolated CSS
Each `.razor.css` file handles its own responsive breakpoints:

```css
/* PostCard.razor.css */
.blog-post {
    padding: 1.5rem;
}

@media (max-width: 640.98px) {
    .blog-post {
        padding: 1rem;
    }
}
```

### Shared Breakpoint Values
Use SCSS variables in global files, hard-code values in isolated CSS:

```scss
// _variables.scss
$breakpoint-mobile: 640.98px;

// Any .razor.css file (can't import SCSS)
@media (max-width: 640.98px) {
    /* mobile styles */
}
```

## Migration Strategy

### Converting Global to Isolated
1. Identify component-specific styles in `blog.scss`
2. Create matching `.razor.css` file
3. Copy relevant styles
4. Remove from `blog.scss`
5. Test component still looks correct
6. Commit changes

## Performance Considerations

### Build Output
Blazor bundles all `.razor.css` files into:
```
Viblog.styles.css
```

This file is automatically referenced in `App.razor`:
```html
<link rel="stylesheet" href="Viblog.styles.css" />
```

### Loading
- Single HTTP request for all scoped styles
- Minified in production
- Cached by browser
- No runtime overhead

## Debugging

### Inspecting Scoped Styles
In browser DevTools:
```html
<article class="blog-post" b-abc123>
```

Look for the `b-xxxxxx` attribute to identify the scope.

### Finding Styles
1. Inspect element in DevTools
2. Look for styles ending with `[b-xxxxxx]`
3. The scope ID matches the component file name
4. Styles come from `Viblog.styles.css`

## Future Enhancements

Potential improvements:
- **CSS Modules**: If need more advanced features
- **Tailwind CSS**: For utility-first approach
- **CSS-in-JS**: For dynamic theming
- **Design Tokens**: For design system integration

## Related Files
- All `.razor.css` files in `Viblog\Frontend\`
- `Viblog\wwwroot\blog.scss` - Global utilities
- `Viblog\wwwroot\styles\_variables.scss` - Shared variables
- `Viblog\wwwroot\styles\_mixins.scss` - Shared mixins
- `Viblog\Components\App.razor` - References `Viblog.styles.css`

## References
- [Blazor CSS Isolation Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)
- [CSS Scoping Specification](https://www.w3.org/TR/css-scoping-1/)
