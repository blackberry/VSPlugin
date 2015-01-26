#include <bb/Application>

#include "Service.hpp"

using namespace bb;

int main(int argc, char **argv)
{
    Application app(argc, argv);

    // Create the Application UI object, this is where the main.qml file
    // is loaded and the application scene is set.
    Service srv;

    // Enter the application main event loop.
    return Application::exec();
}
