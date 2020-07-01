import React, { Component, Fragment } from 'react';
import { connect } from 'react-redux';
import Notification from '../Components/Notification';
import { hide } from '../Actions/Notification.action';

class NotificationContainer extends Component {
    constructor(props) {
        super(props);
        this.state = {
            timers: [],
        };
    }

    componentDidUpdate(prevProps) {
        if (this.props.notification.hash === prevProps.notification.hash) {
            return;
        }

        // set timeout to hide the notifications
        const notificationIds = Object.keys(this.props.notification.byId);
        const timersToRemove = this.state.timers.filter(timer => !notificationIds.includes(timer.id));
        const timersToAdd = notificationIds.filter(id => !this.state.timers.find(timer => timer.id === id));
        timersToRemove.forEach(timer => clearTimeout(timer.handle));
        if (!timersToAdd || !timersToAdd.length) {
            this.setState({ timers: [] });
            return;
        }
        const timers = [];
        timersToAdd.forEach(id => {
            timers.push({ id, handle: setTimeout(() => this.props.hide(id, Date.now()), 2000) });
        });
        this.setState({ timers });
    }

    render() {
        const ids = Object.keys(this.props.notification.byId);
        const notifications = ids.map(id => ({ ...this.props.notification.byId[id], id }));
        return <Fragment>
            {notifications && notifications.map(notification => <Notification key={notification.id} text={notification.text} {...this.getPosition(notification.id)} />)}
        </Fragment>
    }

    getPosition(nodeId) {
        const domNode = document.getElementById(nodeId);
        if (!domNode) {
            return;
        }
        let element;
        const scrollLeft = window.scrollX ||  (((element = document.documentElement) || (element = document.body.parentNode))
                            && typeof element.scrollLeft == 'number' ? element : document.body).scrollLeft;
        const scrollTop = window.scrollY || (((element = document.documentElement) || (element = document.body.parentNode))
                            && typeof element.scrollTop == 'number' ? element : document.body).scrollTop;
        const { top, left } = domNode.getBoundingClientRect();

        const maxWidth = document.documentElement.clientWidth;
        // Use right position if the position exceeds the max width of screen 
        // (assume the width of notification is the max width / 3)
        if (left + scrollLeft + maxWidth / 3 > maxWidth) {
            return { top: `${top + scrollTop}px`, right: `${maxWidth - (left + scrollLeft)}px`, display: 'block' };
        }
        return { top: `${top + scrollTop}px`, left: `${left + scrollLeft}px`, display: 'block' };
    }
}

const mapStateToProps = state => {
    return {
        notification: state.notification,
    }
}

const mapDispatchToProps = {
    hide,
}

export default connect(mapStateToProps, mapDispatchToProps)(NotificationContainer);