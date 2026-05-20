# Unity UI Rules

Rules:
- parent defines bounds
- child stays inside parent
- siblings must not overlap
- repeated layouts use LayoutGroup
- long lists use ScrollRect
- scalable UI uses min/max size

Prefer:
- VerticalLayoutGroup
- HorizontalLayoutGroup
- GridLayoutGroup
- ContentSizeFitter
- LayoutElement
- RectMask2D
- responsive anchors

Avoid:
- manual position hacks
- unstable layouts
- absolute positioning abuse