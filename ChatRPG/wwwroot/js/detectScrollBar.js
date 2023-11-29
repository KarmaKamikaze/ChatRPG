export function DetectScrollBar() {
    // Check if the body has a vertical scrollbar
    return document.body.scrollHeight > window.innerHeight;
}
