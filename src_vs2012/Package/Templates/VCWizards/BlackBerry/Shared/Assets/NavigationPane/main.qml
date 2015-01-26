import bb.cascades $CascadesVersion$

NavigationPane {
    id: navigationPane

    Page {
        titleBar: TitleBar {
            // Localized text with the dynamic translation and locale updates support
            title: qsTr("Page 1") + Retranslate.onLocaleOrLanguageChanged
        }

        Container {
        }

        actions: ActionItem {
            title: qsTr("Second page") + Retranslate.onLocaleOrLanguageChanged
            ActionBar.placement: ActionBarPlacement.OnBar

            onTriggered: {
                // A second Page is created and pushed when this action is triggered.
                navigationPane.push(secondPageDefinition.createObject());
            }
        }
    }

    attachedObjects: [
        // Definition of the second Page, used to dynamically create the Page above.
        ComponentDefinition {
            id: secondPageDefinition
            source: "DetailsPage.qml"
        }
    ]

    onPopTransitionEnded: {
        // Destroy the popped Page once the back transition has ended.
        page.destroy();
    }
}
