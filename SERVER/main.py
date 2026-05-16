import mss
import numpy as np
import keyboard
import pydirectinput
import time

# --- CONFIGURATION ---
# Target: Vibrant Purple (193, 51, 235)
TARGET_COLOR = np.array([193, 51, 235])
TOLERANCE = 15

# Movement Settings
TP_PERCENT = 0.6  # Snap 60% of the way
GLIDE_STEPS = 6  # Smoothly travel the rest


def hybrid_move(target_x, target_y):
    current_x, current_y = pydirectinput.position()
    diff_x, diff_y = target_x - current_x, target_y - current_y

    # Phase 1: Snap (TP)
    tp_x = int(current_x + (diff_x * TP_PERCENT))
    tp_y = int(current_y + (diff_y * TP_PERCENT))
    pydirectinput.moveTo(tp_x, tp_y)
    time.sleep(0.01)

    # Phase 2: Glide
    if GLIDE_STEPS > 0:
        x_stride = (target_x - tp_x) / GLIDE_STEPS
        y_stride = (target_y - tp_y) / GLIDE_STEPS
        for i in range(GLIDE_STEPS):
            pydirectinput.moveTo(int(tp_x + (x_stride * (i + 1))),
                                 int(tp_y + (y_stride * (i + 1))))
            time.sleep(0.005)  # Slightly faster glide interval


def scan_and_click(sct, monitor):
    # Capture the entire first monitor
    img = np.array(sct.grab(monitor))

    # MSS returns BGRA; slice to RGB and flip to standard order
    img_rgb = img[:, :, :3][:, :, ::-1]

    # Instant NumPy vector matching
    lower_bound = TARGET_COLOR - TOLERANCE
    upper_bound = TARGET_COLOR + TOLERANCE

    mask = np.all((img_rgb >= lower_bound) & (img_rgb <= upper_bound), axis=-1)
    matches = np.argwhere(mask)

    if len(matches) > 0:
        # We'll pick a match toward the middle of the found pixels for better accuracy
        target_y, target_x = matches[len(matches) // 2]

        # Convert to global screen coordinates
        # monitor['left'] and monitor['top'] handle multi-monitor offsets
        screen_x = monitor["left"] + target_x
        screen_y = monitor["top"] + target_y

        print(f"Target found at {screen_x}, {screen_y}. Executing move...")
        hybrid_move(screen_x, screen_y)
        pydirectinput.click()
        return True
    return False


print("--- FULL MONITOR SCAN ACTIVE ---")
print("Target: Vibrant Purple | Stop Key: Q")

with mss.mss() as sct:
    # monitor[1] is the primary monitor. monitor[0] is the entire desktop.
    main_monitor = sct.monitors[1]

    try:
        while True:
            # Emergency Stop
            if keyboard.is_pressed('q'):
                print("Exiting...")
                break

            scan_and_click(sct, main_monitor)

            # Responsive 3-second wait
            for _ in range(30):
                if keyboard.is_pressed('q'): break
                time.sleep(0.1)

    except KeyboardInterrupt:
        print("Script stopped.")