#include <bps/bps.h>
#include <bps/event.h>
#include <bps/navigator.h>
#include <bps/screen.h>
#include <fcntl.h>
#include <screen/screen.h>

static bool shutdown = false;


static void handle_screen_event(bps_event_t *event)
{
    int screen_val;

    screen_event_t screen_event = screen_event_get_event(event);
    screen_get_event_property_iv(screen_event, SCREEN_PROPERTY_TYPE, &screen_val);

    switch (screen_val)
    {
        case SCREEN_EVENT_MTOUCH_TOUCH:
            fprintf(stderr,"Touch event");
            break;
        case SCREEN_EVENT_MTOUCH_MOVE:
            fprintf(stderr,"Move event");
            break;
        case SCREEN_EVENT_MTOUCH_RELEASE:
            fprintf(stderr,"Release event");
            break;
        default:
            break;
    }
    fprintf(stderr,"\n");
}

static void handle_navigator_event(bps_event_t *event)
{
    switch (bps_event_get_code(event))
    {
        case NAVIGATOR_SWIPE_DOWN:
            fprintf(stderr,"Swipe down event");
            break;
        case NAVIGATOR_EXIT:
            fprintf(stderr,"Exit event");
            shutdown = true;
            break;
        default:
            break;
    }
    fprintf(stderr,"\n");
}

static void handle_event()
{
    int domain;

    bps_event_t *event = NULL;
    if (BPS_SUCCESS != bps_get_event(&event, -1))
    {
        fprintf(stderr, "bps_get_event() failed\n");
        return;
    }

    if (event)
    {
        domain = bps_event_get_domain(event);
        if (domain == navigator_get_domain())
        {
            handle_navigator_event(event);
        }
        if (domain == screen_get_domain())
        {
            handle_screen_event(event);
        }
    }
}

/**
 * Application Entry Point.
 */
int main(int argc, char **argv)
{
    const int usage = SCREEN_USAGE_NATIVE;

    screen_context_t screen_ctx;
    screen_window_t screen_win;
    screen_buffer_t screen_buf = NULL;
    int rect[4] = { 0, 0, 0, 0 };
    char window_group_id[16] = "";

    /* Use the PID to set the window group id */
    snprintf(window_group_id, sizeof(window_group_id), "%d", getpid());

    /* Setup the window */
    screen_create_context(&screen_ctx, 0);
    screen_create_window(&screen_win, screen_ctx);
    screen_create_window_group(screen_win, window_group_id);
    screen_set_window_property_iv(screen_win, SCREEN_PROPERTY_USAGE, &usage);
    screen_create_window_buffers(screen_win, 1);

    screen_get_window_property_pv(screen_win, SCREEN_PROPERTY_RENDER_BUFFERS, (void **)&screen_buf);
    screen_get_window_property_iv(screen_win, SCREEN_PROPERTY_BUFFER_SIZE, rect+2);

    /* Fill the screen buffer with blue */
    int attribs[] = { SCREEN_BLIT_COLOR, 0xff0000ff, SCREEN_BLIT_END };
    screen_fill(screen_ctx, screen_buf, attribs);
    screen_post_window(screen_win, screen_buf, 1, rect, 0);

    /* Signal bps library that navigator and screen events will be requested */
    bps_initialize();
    screen_request_events(screen_ctx);
    navigator_request_events(0);

    while (!shutdown)
    {
        /* Handle user input */
        handle_event();
    }

    /* Clean up */
    screen_stop_events(screen_ctx);
    bps_shutdown();
    screen_destroy_window(screen_win);
    screen_destroy_context(screen_ctx);
    return 0;
}
