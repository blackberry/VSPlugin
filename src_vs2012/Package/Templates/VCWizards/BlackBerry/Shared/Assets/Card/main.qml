import bb.cascades $CascadesVersion$

Page {
    Container {
        Label {
            // Localized text with the dynamic translation and locale updates support
            text: qsTr("Send memo to card") + Retranslate.onLocaleOrLanguageChanged
            textStyle.base: SystemDefaults.TextStyles.BigText
        }
        TextField {
            id: tf
            text: "Hello ..."
        }
        Button {
            text: "Send & reveal card"
            onClicked: {
                ApplicationUI.invokeCard(tf.text);
            }
        }
    }
}
