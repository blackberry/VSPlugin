import bb.cascades $CascadesVersion$

TabbedPane {
    id: tabbedPane

    Tab {
        title: "Tab 1"

        content: Page {
            Label {
                text: "This is tab 1."
            }
        }
    }

    Tab {
        title: "Tab 2"

        content: Page {
            Label {
                text: "This is tab 2."
            }
        }
    }
}
