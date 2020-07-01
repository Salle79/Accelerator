import { NOTIFICATION_SHOW, NOTIFICATION_HIDE } from '../Actions/Notification.action';

export const notification = (state = { byId: {}, hash: '' }, action) => {
    const { type, payload } = action;
    switch (type) {
        case NOTIFICATION_SHOW:
            return {
                ...state,
                byId: {
                    ...state.byId,
                    [payload.nodeId]: {
                        text: payload.text,
                    }
                },
                hash: payload.hash,
            };
        case NOTIFICATION_HIDE:
            const newState = {...state, hash: payload.hash};
            delete newState.byId[payload.nodeId];
            return newState;
        default:
            return state;
    }
}