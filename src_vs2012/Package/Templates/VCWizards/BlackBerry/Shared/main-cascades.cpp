#include <bb/cascades/Application>
#include <Qt/qdeclarativedebug.h>

#include "ApplicationUI.hpp"

using namespace bb::cascades;

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
 * Application Entry Point.
 */
Q_DECL_EXPORT int main(int argc, char **argv)
{
    Application app(argc, argv);

#if _DEBUG
    qInstallMsgHandler(WriteLogsToConsole);
#endif /* _DEBUG */

    // Create the Application UI object, this is where the main.qml file
    // is loaded and the application scene is set.
    ApplicationUI appui;

    // Enter the application main event loop.
    return Application::exec();
}
