#include <bb/cascades/Application>
#include <bb/cascades/LocaleHandler>
#include <bb/system/InvokeManager>

#include <Qt/qdeclarativedebug.h>

#include "ApplicationUI.hpp"
#include "CardUI.hpp"

using namespace bb::cascades;
using namespace bb::system;

Q_DECL_EXPORT int main(int argc, char **argv)
{
    Application app(argc, argv);

    InvokeManager invokeManager;

    QObject *appui = 0;
    if (invokeManager.startupMode() == ApplicationStartupMode::InvokeCard)
    {
        // Create the Card UI object
        appui = new CardUI(&invokeManager);
    }
    else
    {
        // Create the Application UI object, this is where the main.qml file
        // is loaded and the application scene is set.
        appui = new ApplicationUI(&invokeManager);
    }

    // Enter the application main event loop.
    int ret = Application::exec();

    invokeManager.closeChildCard();
    delete appui;

    return ret;
}
