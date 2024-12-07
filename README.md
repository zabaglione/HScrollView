
# Horizontal Center Scroll

A Unity script that enables a smooth horizontal scroll view where the central element is automatically highlighted and scaled up during navigation (via buttons or drag). It works with `ScrollRect` and supports animations using [DOTween](http://dotween.demigiant.com/).

## Features
- Automatically centers and scales the element in focus.
- Smooth animations for scaling and scrolling using DOTween.
- Supports button-based navigation (`Next`/`Prev`) and drag-based navigation.
- No manual setup of ScrollRect components required; the script auto-detects required components.

---

## Requirements
- Unity 2021.3 or later (tested on LTS versions).
- DOTween plugin for animations ([Download DOTween](http://dotween.demigiant.com/)).

---

## Installation
1. Clone or download the repository.
2. Copy the `HorizontalCenterScroll.cs` script to your Unity project.
3. Import and set up DOTween in your Unity project if not already installed.

---

## How to Use
1. **Setup the Scroll View**:
   - Create a horizontal `ScrollRect` in your Unity scene.
   - Set the `ScrollRect` with the following hierarchy:
     ```
     - Scroll View (GameObject with ScrollRect component)
       - Viewport (child RectTransform of ScrollRect)
         - Content (child RectTransform of Viewport, with HorizontalLayoutGroup)
           - Element1 (child RectTransform of Content)
           - Element2 (child RectTransform of Content)
           ...
     ```
   - Add a `HorizontalLayoutGroup` component to the `Content` GameObject.

2. **Attach the Script**:
   - Attach `HorizontalCenterScroll` to the GameObject with the `ScrollRect`.

3. **Add Elements**:
   - Add your UI elements (e.g., images, buttons, etc.) as children of the `Content` GameObject.
   - Ensure all elements are of the same size for consistent padding.

4. **Navigation Buttons** (Optional):
   - Add UI buttons for `Next` and `Prev` navigation.
   - Hook the buttons to `OnNextButton` and `OnPrevButton` methods of the `HorizontalCenterScroll` script.

---

## Example Scene
You can refer to the example scene included in the repository for a complete setup.

---

## Known Issues
- Ensure all elements in the content have the same size for the best results.
- If DOTween is not installed, animations will not work, and you may encounter runtime errors.

---

## Contributing
Contributions are welcome! Feel free to fork the repository and submit pull requests.

---

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

## Contact
For questions or feedback, please create an issue or contact the maintainer.
