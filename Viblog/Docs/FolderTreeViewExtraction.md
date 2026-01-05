# FolderTreeView Component Extraction

## ? Completed: TreeView Component Extraction

Successfully extracted duplicated Telerik TreeView code into a reusable `FolderTreeView` component.

---

## ?? New Component Created

### **FolderTreeView.razor**
**Location:** `Viblog/Admin/Components/Media/FolderTreeView.razor`

**Purpose:** Reusable folder tree component using Telerik TreeView with standardized bindings

---

## ?? Component API

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `FolderTreeData` | `List<FolderNode>?` | `null` | Folder hierarchy data for the tree |
| `SelectedFolders` | `IEnumerable<FolderNode>` | `Empty` | Currently selected folders |
| `SelectedFoldersChanged` | `EventCallback<IEnumerable<FolderNode>>` | - | Callback when selection changes |
| `SelectionMode` | `TreeViewSelectionMode` | `Single` | Single or Multiple selection mode |
| `OnFolderClicked` | `EventCallback<TreeViewItemClickEventArgs>` | - | Optional callback for folder clicks |

---

## ?? Usage Examples

### Example 1: Single-Select Tree for Navigation (MediaLibrary)

```razor
<FolderTreeView FolderTreeData="@_folderTreeData"
               SelectedFolders="@_selectedFolders"
               SelectedFoldersChanged="@((folders) => _selectedFolders = folders)"
               OnFolderClicked="@OnFolderClick" />
```

**Features:**
- Single folder selection (default mode)
- Click handler for navigation
- Two-way binding for selected folders

---

### Example 2: Single-Select Tree for Dialog (MoveToFolderDialog)

```razor
<FolderTreeView FolderTreeData="@FolderTreeData"
               SelectedFolders="@_selectedFolders"
               SelectedFoldersChanged="@((folders) => _selectedFolders = folders)" />
```

**Features:**
- Single folder selection (default mode)
- No click handler needed
- Simplified for folder picker scenario

---

### Example 3: Multi-Select Tree (Advanced Scenario)

```razor
<FolderTreeView FolderTreeData="@_folderTreeData"
               SelectedFolders="@_selectedFolders"
               SelectedFoldersChanged="@((folders) => _selectedFolders = folders)"
               SelectionMode="@TreeViewSelectionMode.Multiple"
               OnFolderClicked="@OnFolderClick" />
```

**Features:**
- Multiple folder selection
- Useful for bulk operations on folders
- Explicitly set SelectionMode to Multiple

---

## ?? Code Before vs After

### Before (Duplicated Code)

**MediaLibrary.razor:**
```razor
<TelerikTreeView Data="@_folderTreeData"
               SelectedItems="@_selectedFolders"
               SelectedItemsChanged="@((IEnumerable<object> items) => _selectedFolders = items.Cast<FolderNode>())"
               OnItemClick="@OnFolderClick">
    <TreeViewBindings>
        <TreeViewBinding TextField="Name" ParentIdField="ParentPath" IdField="Path" />
    </TreeViewBindings>
</TelerikTreeView>
```

**MoveToFolderDialog.razor:**
```razor
<TelerikTreeView Data="@FolderTreeData"
               SelectedItems="@_selectedFolders"
               SelectedItemsChanged="@OnFolderSelectionChanged"
               SelectionMode="@TreeViewSelectionMode.Single">
    <TreeViewBindings>
        <TreeViewBinding TextField="Name" ParentIdField="ParentPath" IdField="Path" />
    </TreeViewBindings>
</TelerikTreeView>

// Plus handler method:
private void OnFolderSelectionChanged(IEnumerable<object> items)
{
    _selectedFolders = items.Cast<FolderNode>();
}
```

**Issues:**
- ? Duplicated TreeView configuration
- ? Duplicated bindings setup
- ? Different selection handling approaches
- ? Harder to maintain consistency

---

### After (Extracted Component)

**MediaLibrary.razor:**
```razor
<FolderTreeView FolderTreeData="@_folderTreeData"
               SelectedFolders="@_selectedFolders"
               SelectedFoldersChanged="@((folders) => _selectedFolders = folders)"
               OnFolderClicked="@OnFolderClick" />
```

**MoveToFolderDialog.razor:**
```razor
<FolderTreeView FolderTreeData="@FolderTreeData"
               SelectedFolders="@_selectedFolders"
               SelectedFoldersChanged="@((folders) => _selectedFolders = folders)"
               SelectionMode="@TreeViewSelectionMode.Single" />
```

**Benefits:**
- ? Single source of truth for TreeView configuration
- ? Consistent bindings and behavior
- ? Simplified usage code
- ? Easy to maintain and enhance
- ? Removed handler boilerplate from dialog

---

## ?? Component Features

### 1. **Automatic Type Casting**
The component handles the cast from `IEnumerable<object>` to `IEnumerable<FolderNode>` internally, so consumers don't need to worry about it.

### 2. **Flexible Selection Modes**
Supports both single and multiple selection via the `SelectionMode` parameter:
- **`TreeViewSelectionMode.Single` (default)** - Pick one folder
- `TreeViewSelectionMode.Multiple` - Select multiple folders (must be explicitly set)

### 3. **Optional Click Handler**
The `OnFolderClicked` event is optional. Use it when you need to respond to clicks (like navigation), omit it for simple selection scenarios.

### 4. **Two-Way Binding Pattern**
Uses standard Blazor two-way binding pattern:
```razor
SelectedFolders="@_selectedFolders"
SelectedFoldersChanged="@((folders) => _selectedFolders = folders)"
```

### 5. **Consistent Bindings**
TreeView bindings are centralized in the component:
- `TextField="Name"` - Display folder name
- `ParentIdField="ParentPath"` - Hierarchy relationship
- `IdField="Path"` - Unique identifier

---

## ?? Component Implementation Details

### Internal State
```csharp
private IEnumerable<FolderNode> _selectedFolders = Enumerable.Empty<FolderNode>();
```

The component maintains internal state synchronized with the parent via two-way binding.

### Selection Change Handler
```csharp
private async Task OnSelectionChanged(IEnumerable<object> items)
{
    _selectedFolders = items.Cast<FolderNode>();
    await SelectedFoldersChanged.InvokeAsync(_selectedFolders);
}
```

Automatically casts Telerik's object collection to typed `FolderNode` collection.

### Click Handler
```csharp
private async Task OnItemClick(TreeViewItemClickEventArgs args)
{
    if (OnFolderClicked.HasDelegate)
    {
        await OnFolderClicked.InvokeAsync(args);
    }
}
```

Only invokes if parent provided a handler, making it truly optional.

---

## ?? Impact Metrics

### Code Reduction

**MediaLibrary.razor:**
- Before: 10 lines of TreeView markup
- After: 5 lines using FolderTreeView
- **Reduction:** 50%

**MoveToFolderDialog.razor:**
- Before: 10 lines TreeView + 5 lines handler method = 15 lines
- After: 5 lines using FolderTreeView
- **Reduction:** 67%

**Total:**
- Lines eliminated: ~20 lines of duplicated code
- New component: 35 lines (single source of truth)
- Net benefit: Consolidation + consistency

---

## ? Benefits Summary

### 1. **DRY Principle**
- Single source of truth for folder tree configuration
- No duplicated TreeView setup code
- Easier to enhance tree functionality globally

### 2. **Consistency**
- Same tree behavior everywhere
- Uniform bindings and selection handling
- Predictable user experience

### 3. **Maintainability**
- Changes to tree behavior in one place
- Easy to add features (e.g., icons, context menu)
- Clear component responsibility

### 4. **Testability**
- Component can be unit tested independently
- Clear parameter contracts
- Isolated selection logic

### 5. **Reusability**
- Can be used in future dialogs/pages
- Configurable via parameters
- Adaptable to different scenarios

---

## ?? Future Enhancements

The extracted component makes it easy to add features globally:

### 1. **Folder Icons**
```razor
<TreeViewBindings>
    <TreeViewBinding TextField="Name" 
                    ParentIdField="ParentPath" 
                    IdField="Path"
                    IconField="Icon" />
</TreeViewBindings>
```

### 2. **Context Menu Support**
```csharp
[Parameter]
public EventCallback<TreeViewItemClickEventArgs> OnFolderContextMenu { get; set; }
```

### 3. **Expand/Collapse State**
```csharp
[Parameter]
public List<string> ExpandedPaths { get; set; }
```

### 4. **Custom Templates**
```razor
<ItemTemplate>
    @{
        var folder = context as FolderNode;
        <span class="@(folder.IsUiOnly ? "ui-only" : "")">@folder.Name</span>
    }
</ItemTemplate>
```

### 5. **Drag & Drop**
Enable folder reorganization via drag and drop in the tree.

---

## ?? Build Status

? **Build Successful**  
? **All Components Compile**  
? **No Breaking Changes**  
? **Usage Simplified**

---

## ?? Related Files

**Component Files:**
- `FolderTreeView.razor` - The reusable component

**Usage Locations:**
- `MediaLibrary.razor` - Main folder tree panel
- `MoveToFolderDialog.razor` - Move destination picker

**Supporting Files:**
- `MediaLibraryModels.cs` - `FolderNode` definition

---

## ?? Migration Notes

### For Developers

When adding new uses of folder trees:

**? Don't:**
```razor
<!-- Don't duplicate TreeView code -->
<TelerikTreeView Data="@folders">
    <TreeViewBindings>
        <TreeViewBinding TextField="Name" ...>
    </TreeViewBindings>
</TelerikTreeView>
```

**? Do:**
```razor
<!-- Use the shared component -->
<FolderTreeView FolderTreeData="@folders"
               SelectedFolders="@_selected"
               SelectedFoldersChanged="@((f) => _selected = f)" />
```

---

## ?? Summary

**What Changed:**
- Extracted Telerik TreeView into `FolderTreeView` component
- Updated MediaLibrary and MoveToFolderDialog to use new component
- Removed duplicated tree configuration code

**Why It Matters:**
- Single source of truth for folder trees
- Easier to maintain and enhance
- Consistent behavior across the application
- Follows DRY and Single Responsibility principles

**Result:**
- Cleaner code
- Better maintainability
- Enhanced reusability
- Consistent user experience

---

**Extraction Complete:** ?  
**Impact:** Improved code organization and maintainability through component reuse
