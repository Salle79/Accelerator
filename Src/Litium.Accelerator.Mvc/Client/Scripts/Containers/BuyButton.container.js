import React, { Component } from 'react';
import { connect } from 'react-redux';
import BuyButton from '../Components/BuyButton';
import { add as addToCart } from '../Actions/Cart.action';
import { translate } from '../Services/translation';

class BuyButtonContainer extends Component {
    constructor(props) {
        super(props);
        this.buttonRef = React.createRef();
    }

    render() {
        return (
            <span ref={this.buttonRef}>
                <BuyButton {...this.props} 
                    onClick={(articleNumber, quantityFieldId) => this.props.addToCart(this.buttonRef.current, articleNumber, quantityFieldId)} />
            </span>
        );
    }
}

const mapStateToProps = state => {
    return { }
}

const mapDispatchToProps = dispatch => {
    return {
        addToCart: (buttonDomNode, articleNumber, quantityFieldId) => {
            const nodeIdToShowNotification = buttonDomNode.id = Date.now();
            dispatch(addToCart({ 
                articleNumber, 
                quantity: quantityFieldId ? document.getElementById(quantityFieldId).value : 1,
                nodeIdToShowNotification,
                notificationMessage: translate('tooltip.addedtocart'),
                hash: Date.now(),
            }));
        }
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(BuyButtonContainer);