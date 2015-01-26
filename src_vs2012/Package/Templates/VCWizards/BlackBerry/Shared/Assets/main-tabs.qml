import bb.cascades $CascadesVersion$

TabbedPane {
    showTabsOnActionBar: true
    Tab {
        //First tab
        // Localized text with the dynamic translation and locale updates support
        title: qsTr("Tab 1") + Retranslate.onLocaleOrLanguageChanged
        Page {
            Container {
                Label {
                    text: qsTr("First tab") + Retranslate.onLocaleOrLanguageChanged
                }
            }
        }
    }

    Tab {
        //Second tab
        title: qsTr("Tab 2") + Retranslate.onLocaleOrLanguageChanged
        Page {
            Container {
                Label {
                    text: qsTr("Second tab") + Retranslate.onLocaleOrLanguageChanged
                }
            }
        }
    }
}
