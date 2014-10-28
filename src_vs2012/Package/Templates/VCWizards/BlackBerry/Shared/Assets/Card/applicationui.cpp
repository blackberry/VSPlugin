#include <bb/cascades/Application>
#include <bb/cascades/QmlDocument>
#include <bb/cascades/AbstractPane>
#include <bb/system/CardDoneMessage>
#include <bb/system/InvokeManager>

#include "ApplicationUI.hpp"

using namespace bb::cascades;
using namespace bb::system;

ApplicationUI::ApplicationUI(InvokeManager *invokeManager)
    : ApplicationUIBase(invokeManager)
{
    bool res = connect(m_pInvokeManager, SIGNAL(childCardDone(const bb::system::CardDoneMessage&)), this, SLOT(cardDone(const bb::system::CardDoneMessage&)));
    Q_ASSERT(res);

    // Since the variable is not used in the app, this is added to avoid a
    // compiler warning
    Q_UNUSED(res);

    // Create scene document from main.qml asset, the parent is set
    // to ensure the document gets destroyed properly at shut down.
    QmlDocument *qml = QmlDocument::create("asset:///main.qml").parent(this);
    // Make app UI available to the qml.
    qml->setContextProperty("ApplicationUI", this);

    // Create root object for the UI
    AbstractPane *root = qml->createRootObject<AbstractPane>();

    // Set created root object as the application scene
    Application::instance()->setScene(root);
}

void ApplicationUI::invokeCard(const QString &memo)
{
    InvokeRequest cardRequest;
    cardRequest.setTarget("com.$AuthorSafe$.$ProjectName$");
    cardRequest.setAction("bb.action.VIEW");
    cardRequest.setMimeType("application/text");
    cardRequest.setData(memo.toUtf8());
    m_pInvokeManager->invoke(cardRequest);
}

void ApplicationUI::cardDone(const bb::system::CardDoneMessage &doneMessage)
{
    qDebug() << "cardDone: " << doneMessage.reason();
}
