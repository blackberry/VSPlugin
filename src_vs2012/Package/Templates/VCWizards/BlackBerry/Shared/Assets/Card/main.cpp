#include <bb/cascades/Application>
#include <bb/cascades/LocaleHandler>
#include <bb/system/InvokeManager>

#include <Qt/qdeclarativedebug.h>

#include "ApplicationUI.hpp"
#include "CardUI.hpp"

using namespace bb::cascades;
using namespace bb::system;

#if _DEBUG
/**
 * Redirect all Cascades logs on standard console.
 */
static void WriteLogsToConsole(QtMsgType type, const char *message)
{
    Q_UNUSED(type);
    std::fprintf(stdout, "%s\n", message);
    std::fflush(stdout);
}
#endif /* _DEBUG */


/**
 * Cascades Application Entry Point
 */
Q_DECL_EXPORT int main(int argc, char **argv)
{
    Application app(argc, argv);

    InvokeManager invokeManager;

#if _DEBUG
    qInstallMsgHandler(WriteLogsToConsole);
#endif /* _DEBUG */

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
