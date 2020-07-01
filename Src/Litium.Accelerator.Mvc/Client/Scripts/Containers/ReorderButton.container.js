import React, { Component } from 'react';
import { connect } from 'react-redux';
import ReorderButton from '../Components/ReorderButton';
import { reorder } from '../Actions/Cart.action';
import { translate } from '../Services/translation';

class ReorderButtonContainer extends Component {
    constructor(props) {
        super(props);
        this.buttonRef = React.createRef();
    }

    render() {
        return (
            <span ref={this.buttonRef}>
                <ReorderButton {...this.props} 
                    onClick={(orderId) => this.props.reorder(this.buttonRef.current, orderId)} />
            </span>
        );
    }
}

const mapStateToProps = state => {
    return { }
}

const mapDispatchToProps = dispatch => {
    return {
        reorder: (buttonDomNode, orderId) => {
            const nodeIdToShowNotification = buttonDomNode.id = Date.now();
            dispatch(reorder({ 
                orderId, 
                nodeIdToShowNotification,
                notificationMessage: translate('tooltip.reordered'),
                hash: Date.now(),
            }));
        }
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(ReorderButtonContainer);