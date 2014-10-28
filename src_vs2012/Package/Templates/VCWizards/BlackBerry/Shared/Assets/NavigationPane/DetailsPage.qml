import bb.cascades $CascadesVersion$

Page {
    titleBar: TitleBar {
        // Localized text with the dynamic translation and locale updates support
        title: qsTr("Second Page") + Retranslate.onLocaleOrLanguageChanged
    }
    Container {
    }
}
