# SASS Setup for Vilog

This document describes the SASS compilation setup for the Vilog blogging platform.

## Overview

The project uses **AspNetCore.SassCompiler** to automatically compile SCSS files to CSS during the build process. Bootstrap has been removed and replaced with custom SASS stylesheets.

## Structure

```
Vilog/
??? wwwroot/
?   ??? styles/
?   ?   ??? _variables.scss    # Global variables (colors, spacing, breakpoints)
?   ?   ??? _mixins.scss        # Reusable mixins and functions
?   ??? app.scss                # Main application styles
?   ??? blog.scss               # Blog-specific styles
?   ??? app.css                 # Compiled from app.scss (auto-generated)
?   ??? blog.css                # Compiled from blog.scss (auto-generated)
??? Components/
?   ??? Layout/
?       ??? MainLayout.razor.scss
?       ??? NavMenu.razor.scss
?       ??? ReconnectModal.razor.scss
```

## SASS Files

### Partials (_variables.scss, _mixins.scss)

Located in `wwwroot/styles/`, these files contain:

- **_variables.scss**: Global variables for colors, typography, spacing, breakpoints, and component sizes
- **_mixins.scss**: Reusable mixins for focus rings, flex layouts, transitions, responsive breakpoints, and more

### Main Stylesheets

- **app.scss**: Application-wide styles (forms, buttons, validation, error handling)
- **blog.scss**: Blog-specific styles (header, posts, footer, hero section)

### Component Styles

- **MainLayout.razor.scss**: Layout structure and error UI
- **NavMenu.razor.scss**: Navigation menu and icons
- **ReconnectModal.razor.scss**: Reconnection modal animations and styling

## Compilation

### Automatic Compilation

The **AspNetCore.SassCompiler** package automatically compiles SCSS to CSS during build:

```bash
dotnet build
```

Compiled CSS files are generated in the same directory as their SCSS source files.

### File Watching

In development, SASS files are watched and recompiled automatically on save.

## Usage

### Importing Partials

All main SCSS files should import the shared partials:

```scss
// Import partials
@import 'styles/variables';
@import 'styles/mixins';

// Your styles here
.my-component {
    color: $color-primary;
    @include focus-ring;
}
```

### Variables

Use predefined variables for consistency:

```scss
// Colors
$color-primary
$color-white
$color-black
$color-gray-light

// Spacing
$spacing-xs   // 0.25rem
$spacing-sm   // 0.5rem
$spacing-md   // 1rem
$spacing-lg   // 1.5rem
$spacing-xl   // 2rem
$spacing-xxl  // 3rem

// Breakpoints
$breakpoint-mobile  // 640.98px
$breakpoint-tablet  // 768px
$breakpoint-desktop // 641px
```

### Mixins

Use predefined mixins for common patterns:

```scss
// Focus ring
@include focus-ring;
@include focus-ring($custom-color);

// Center content
@include center-content;
@include center-content(1000px);

// Flex layouts
@include flex-column;
@include flex-row;

// Transitions
@include transition-default;
@include transition-default(transform opacity, 0.3s, ease-in-out);

// Hover effects
@include hover-lift;
@include hover-lift(-5px);

// Responsive breakpoints
@include mobile { /* styles for mobile */ }
@include tablet { /* styles for tablet */ }
@include desktop { /* styles for desktop */ }

// Text truncation
@include text-truncate;

// Visually hidden (accessible)
@include visually-hidden;
```

## Adding New Styles

### 1. Create a new SCSS file

```scss
// wwwroot/features/my-feature.scss
@import '../styles/variables';
@import '../styles/mixins';

.my-feature {
    background-color: $color-white;
    padding: $spacing-lg;
    @include transition-default;
}
```

### 2. Reference the compiled CSS in App.razor

```html
<link rel="stylesheet" href="@Assets["features/my-feature.css"]" />
```

### 3. Build the project

The SCSS will be automatically compiled to CSS during build.

## Scoped Component Styles

Blazor component scoped styles also support SCSS:

1. Create a `.razor.scss` file next to your component
2. The SCSS will be compiled and scoped automatically
3. Use partials for consistency:

```scss
// MyComponent.razor.scss
@import '../../wwwroot/styles/variables';
@import '../../wwwroot/styles/mixins';

.my-component {
    color: $color-primary;
}
```

## Best Practices

### 1. Use Variables

Always use variables instead of hard-coded values:

```scss
// ? Good
color: $color-primary;

// ? Bad
color: #006bb7;
```

### 2. Use Mixins for Repetitive Patterns

```scss
// ? Good
.button {
    @include transition-default;
    @include focus-ring;
}

// ? Bad
.button {
    transition: all 0.2s ease;
    box-shadow: 0 0 0 0.1rem white, 0 0 0 0.25rem #258cfb;
}
```

### 3. Nest Selectors Logically

```scss
.nav-menu {
    background: $color-black;

    .nav-item {
        padding: $spacing-md;

        a {
            color: $color-white;

            &:hover {
                color: $color-primary;
            }
        }
    }
}
```

### 4. Use Responsive Mixins

```scss
.container {
    width: 100%;

    @include desktop {
        max-width: $max-width-content;
        margin: 0 auto;
    }
}
```

### 5. Don't Nest Too Deeply

Keep nesting to 3-4 levels maximum for maintainability.

## Troubleshooting

### SCSS not compiling

1. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

2. Check for SCSS syntax errors in the build output

3. Ensure the file has the `.scss` extension (not `.sass`)

### Changes not showing in browser

1. Clear browser cache (Ctrl+Shift+R or Cmd+Shift+R)
2. Rebuild the project
3. Check that the CSS file was regenerated (check LastWriteTime)

### Import errors

Use the correct relative path for imports:

```scss
// From wwwroot/app.scss
@import 'styles/variables';

// From Components/Layout/MainLayout.razor.scss
@import '../../wwwroot/styles/variables';
```

## Configuration

The SASS compiler configuration is in `Vilog.csproj`:

```xml
<PackageReference Include="AspNetCore.SassCompiler" Version="1.79.4" />
```

### Compiler Options

The compiler uses default options. For custom configuration, create a `compilerconfig.json` file in the project root.

## Migration from Bootstrap

Bootstrap has been completely removed from the project:

- ? Removed: `wwwroot/lib/bootstrap/`
- ? Removed: Bootstrap CSS reference in `App.razor`
- ? Added: Custom SASS files with variables and mixins
- ? Added: AspNetCore.SassCompiler for automatic compilation

All Bootstrap classes should be replaced with custom classes using the Vilog design system.

## Resources

- [AspNetCore.SassCompiler Documentation](https://github.com/koenvzeijl/AspNetCore.SassCompiler)
- [SASS Documentation](https://sass-lang.com/documentation)
- [SASS Guidelines](https://sass-guidelin.es/)
