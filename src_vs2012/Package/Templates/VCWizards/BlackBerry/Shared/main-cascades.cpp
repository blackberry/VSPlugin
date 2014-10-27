#include <bb/cascades/Application>
#include <Qt/qdeclarativedebug.h>

#include "ApplicationUI.hpp"

using namespace bb::cascades;

/**
 * Application Entry Point.
 */
Q_DECL_EXPORT int main(int argc, char **argv)
{
    Application app(argc, argv);

    // Create the Application UI object, this is where the main.qml file
    // is loaded and the application scene is set.
    ApplicationUI appui;

    // Enter the application main event loop.
    return Application::exec();
}
