import { get, post, put } from '../Services/http';
import { catchError } from './Error.action';
import { show as showNotification } from './Notification.action';

export const CART_LOAD = 'CART_LOAD';
export const CART_LOAD_ERROR = 'CART_LOAD_ERROR';
export const CART_RECEIVE = 'CART_RECEIVE';
export const CART_SHOW_INFO = 'CART_SHOW_INFO';
export const CART_HIDE = 'CART_HIDE';

export const load = () => (dispatch, getState) => {
    return get('/api/cart')
        .then(response => response.json())
        .then(cart => dispatch(receive(cart)))
        .catch(ex => dispatch(catchError(ex, error => loadError(error))))
        ;
}

export const loadError = error => ({
    type: CART_LOAD_ERROR,
    payload: {
        error,
    }
})

export const receive = cart => ({
    type: CART_RECEIVE,
    payload: cart,
})

export const toggle = () => (dispatch, getState) => {
    dispatch(show(!getState().cart.showInfo));
}

const show = visible => ({
    type: CART_SHOW_INFO,
    payload: {
        showInfo: visible,
    }
})

export const add = ({ articleNumber, quantity = 1, nodeIdToShowNotification = null, notificationMessage = null, hash = '' }) => {
    return (dispatch) => {
        if (!quantity || isNaN(quantity) || parseInt(quantity) <= 0) {
            return;
        }

        return post('/api/cart/add', { articleNumber, quantity: parseInt(quantity) })
            .then(response => response.json())
            .then(cart => {
                dispatch(receive(cart));
                nodeIdToShowNotification && dispatch(showNotification(nodeIdToShowNotification, notificationMessage, hash));
            })
            .catch(ex => dispatch(catchError(ex, error => loadError(error))))
            ;
    }
}

export const reorder = ({ orderId, nodeIdToShowNotification = null, notificationMessage = null, hash = '' }) => {
    if (orderId) {
        return (dispatch) => {
            return post('/api/cart/reorder', { orderId })
                .then(response => response.json())
                .then(cart => {
                    dispatch(receive(cart));
                    nodeIdToShowNotification && dispatch(showNotification(nodeIdToShowNotification, notificationMessage, hash));
                })
                .catch(ex => dispatch(catchError(ex, error => loadError(error))))
                ;
        }
    }
}

export const update = (rowSystemId, quantity) => (dispatch) => {
    return put(`/api/cart/update`, { rowSystemId, quantity })
        .then(response => response.json())
        .then(cart => dispatch(receive(cart)))
        .catch(ex => dispatch(catchError(ex, error => loadError(error))))
        ;
}