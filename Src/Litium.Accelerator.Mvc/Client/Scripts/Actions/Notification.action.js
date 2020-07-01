export const NOTIFICATION_SHOW = 'NOTIFICATION_SHOW';
export const NOTIFICATION_HIDE = 'NOTIFICATION_HIDE';

export const show = (nodeId, text, hash) => ({
    type: NOTIFICATION_SHOW,
    payload: {
        nodeId,
        text,
        hash,
    }
})

export const hide = (nodeId, hash) => ({
    type: NOTIFICATION_HIDE,
    payload: {
        nodeId,
        hash,
    }
})