export function showDialog(dialogElement, isModal) {
    if (!dialogElement) {
        return;
    }
    
    try {
        if (isModal) {
            dialogElement.showModal();
        } else {
            dialogElement.show();
        }
    } catch (error) {
        console.error('Error showing dialog:', error);
    }
}

export function closeDialog(dialogElement) {
    if (!dialogElement) {
        return;
    }
    
    try {
        dialogElement.close();
    } catch (error) {
        console.error('Error closing dialog:', error);
    }
}
