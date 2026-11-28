# Property-bound input controls

Build the solution (or `Inputs.UI`) to place the input add-in in the application's Addins directory. Add the controls from the **Inputs** group in the document tree. Each control uses an existing property from the property manager; it does not create properties or change their types.

- **Combo box** and **radio button set** use a String property. Select the input in the tree to set its property and enter one option per line in the details panel; the selected option is the property value. Blank lines and duplicate options are ignored. If the current property value is not in the list, no option is selected until the user chooses one.
- **Check box** and **on-off button** use a Boolean property selected in the details panel.
- **Slider** and **potentiometer** use a Double or Integer property and configurable minimum and maximum. The potentiometer supports mouse dragging around the dial and arrow keys. Its details view's portable color pickers edit the foreground, background, and border; the combined foreground/background picker uses its primary color for the foreground and secondary color for the background. The border picker uses its primary color for the border.
- **Joystick** uses two distinct Double or Integer properties, one for each axis, with a shared configurable range (default -1 to 1). Drag the handle or use arrow keys; it holds its last position when released.

User interaction immediately updates the bound properties, and changes made elsewhere are reflected in the controls. Integer writes are rounded and constrained to integers within the configured range. External values outside the range are displayed at the nearest endpoint and reported without changing the property. Missing, incompatible, or non-finite properties disable the control and show an error. Control configuration is saved with the document; changing a property value does not change the control configuration.

The combo box, radio button set, check box, and on-off button use `InputView` for their shared main view and `InputDetails` for their settings. Their implicit templates also allow them to display inside containers.
