# Containers add-in

The **Containers** group contains three projects: `Containers.Interfaces` (the `GridCellPlacement` record and grid limits), `Containers` (placement validation and free-cell rules) and `Containers.UI` (the add-in loaded by Flexible HMI).

## Grid Container

Add a **Grid Container** from the **Containers** group, then add any controls as its children in the tree. The container shows each child's main view in the grid.

- **Rows** and **Columns** set the size of the grid (1 to 100 each, default 2 x 2). A size cannot be reduced below what the current controls occupy.
- **Equal column widths** and **Equal row heights** independently divide the available space equally. Both are enabled by default, including for existing documents. With automatic container height, equal rows are sized to fit the tallest content per row, including controls spanning multiple rows, subject to available space. With fixed height, the height is divided equally and oversized content is clipped. Disable either option to size those tracks to their contents. Content-sized tracks can leave unused space in a fixed-size container, and empty tracks can have zero size; use the tree or child placement editors to place controls in zero-sized tracks.
- **Show grid** displays WPF's dashed grid lines over the controls. It is off by default, including for existing documents, and is saved with the container.
- **Width** and **Height** are device-independent pixels; zero means automatic sizing. **Show grabber** controls the visibility of the child drag handles (shown by default). These settings are saved with the container.
- Each child has a **Row**, **Column**, **Row span** and **Column span** in the container's details view. Rows and columns are zero-based and spans start at 1. A control must stay inside the grid. Controls may overlap.
- Changing a control's row or column reduces its spans so that it still fits; invalid entries are rejected with a message.
- Drag a child's handle to a grid cell to move it; hold Ctrl to copy. Drag the handle to the tree or delete bin to move or delete the child. The tree also supports dragging and Ctrl-drag copying. Moving onto a cell occupied by one child in the same grid swaps their positions. A cell covered by multiple other children, or an occupied cell in another grid, rejects a move. Copies and new controls can still overlap. Spans are retained when possible and reduced at grid edges.
- A new child is placed in the first free cell, scanning row by row. If the grid is full, a row is added.
- Sizes and placements are saved with the document and kept when an item is copied. Placements follow the order of the children.
