import bb.cascades $CascadesVersion$

Page {
    Container {
        Label {
            // Localized text with the dynamic translation and locale updates support
            text: qsTr("Hello World") + Retranslate.onLocaleOrLanguageChanged
            textStyle.base: SystemDefaults.TextStyles.BigText
        }
    }
}
